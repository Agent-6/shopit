using Aspire.Hosting.Yarp.Transforms;
using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var authPostgres = builder.AddPostgres("auth-postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var identityPostgres = builder.AddPostgres("identity-postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var tenancyPostgres = builder.AddPostgres("tenancy-postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var notificationsPostgres = builder.AddPostgres("notifications-postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var catalogPostgres = builder.AddPostgres("catalog-postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var authDb = authPostgres.AddDatabase("auth-db");
var identityDb = identityPostgres.AddDatabase("identity-db");
var tenancyDb = tenancyPostgres.AddDatabase("tenancy-db");
var notificationsDb = notificationsPostgres.AddDatabase("notifications-db");
var catalogDb = catalogPostgres.AddDatabase("catalog-db");

var kafka = builder.AddKafka("kafka")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var seq = builder.AddSeq("seq")
    .ExcludeFromManifest()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("ACCEPT_EULA", "Y");

// Mailpit captures outgoing email in development. SMTP is on 1025 and the web UI
// (http://localhost:8025) is where developers inspect the captured messages.
var mailpit = builder.AddMailPit("mailpit", httpPort: 8025, smtpPort: 1025)
    .WithDataVolume("mailpit-data")
    .WithLifetime(ContainerLifetime.Persistent);

// Shared cache. Redis is the L2 backplane behind each service's in-process HybridCache L1, so an
// invalidation in one service reaches every other process rather than only its own. No data volume:
// a cache should not outlive the process that owns the truth it mirrors.
var redis = builder.AddRedis("cache")
    .WithLifetime(ContainerLifetime.Persistent);

// Client credentials for service-to-service calls are generated per run rather than written down.
// They are the same for every service in one run, which is what makes them work; a fresh value each
// time is what keeps them out of the repository and out of a developer's configuration files. The
// client ids stay in each service's appsettings.json, because an id is not a credential.
var introspectionClientSecret = Guid.NewGuid().ToString("N");
var backendClientSecret = Guid.NewGuid().ToString("N");

static IResourceBuilder<ProjectResource> WithInternalClients(
    IResourceBuilder<ProjectResource> resource,
    string introspectionClientSecret,
    string backendClientSecret) =>
    resource
        .WithEnvironment("InternalClients__Introspection__ClientSecret", introspectionClientSecret)
        .WithEnvironment("InternalClients__Backend__ClientSecret", backendClientSecret);

var auth = builder.AddProject<Projects.ShopIt_Authentication_API>("auth-api")
    .WithReference(authDb)
    .WithReference(seq)
    .WithReference(kafka)
    .WithReference(redis)
    .WaitFor(authDb)
    .WaitFor(kafka)
    .WaitFor(seq)
    .WaitFor(redis);
auth = WithInternalClients(auth, introspectionClientSecret, backendClientSecret);

var identity = builder.AddProject<Projects.ShopIt_Identity_API>("identity-api")
    .WithReference(identityDb)
    .WithReference(seq)
    .WithReference(kafka)
    .WithReference(redis)
    .WaitFor(identityDb)
    .WaitFor(kafka)
    .WaitFor(seq)
    .WaitFor(redis);
identity = WithInternalClients(identity, introspectionClientSecret, backendClientSecret);

auth.WithReference(identity).WaitFor(identity);

var tenancy = builder.AddProject<Projects.ShopIt_Tenancy_API>("tenancy-api")
    .WithReference(tenancyDb)
    .WithReference(seq)
    .WithReference(kafka)
    // Tenancy enforces Identity's permission model: it needs the auth server for
    // client-credentials tokens and the Identity service for permission lookups.
    .WithReference(auth)
    .WithReference(identity)
    .WithReference(redis)
    .WaitFor(tenancyDb)
    .WaitFor(kafka)
    .WaitFor(seq)
    .WaitFor(auth)
    .WaitFor(identity)
    .WaitFor(redis);
tenancy = WithInternalClients(tenancy, introspectionClientSecret, backendClientSecret);

// The catalog service declares catalog.view / catalog.write and publishes them to Identity, and
// validates incoming tokens at the auth server. Like Tenancy it gets both references: introspection
// needs the auth server, and resolving a caller's permissions is an Identity lookup.
var catalog = builder.AddProject<Projects.ShopIt_Catalog_API>("catalog-api")
    .WithReference(catalogDb)
    .WithReference(seq)
    .WithReference(kafka)
    .WithReference(auth)
    .WithReference(identity)
    .WithReference(redis)
    .WaitFor(catalogDb)
    .WaitFor(kafka)
    .WaitFor(seq)
    .WaitFor(auth)
    .WaitFor(identity)
    .WaitFor(redis);
catalog = WithInternalClients(catalog, introspectionClientSecret, backendClientSecret);

// Notifications consumes SendEmailIntegrationEvent from Identity and Authentication
// and delivers the emails to Mailpit's SMTP endpoint. It only needs Kafka, its own
// database (inbox idempotency) and Seq for the delivered-email logs.
var notifications = builder.AddProject<Projects.ShopIt_Notifications_API>("notifications-api")
    .WithReference(notificationsDb)
    .WithReference(seq)
    .WithReference(kafka)
    .WithReference(mailpit)
    .WithReference(redis)
    .WaitFor(notificationsDb)
    .WaitFor(kafka)
    .WaitFor(seq)
    .WaitFor(mailpit)
    .WaitFor(redis);

var gateway = builder.AddYarp("gateway")
    .WithHostPort(5000)
    .WithHostHttpsPort(5001)
    .WithHttpsEndpoint()
    .WithConfiguration(yarp =>
    {
        // Route for Identity API - removes /api/identity prefix
        yarp.AddRoute("/api/identity/{**catch-all}", identity)
            .WithTransformPathRemovePrefix("/api/identity");

        // Route for Authentication API - removes /api/auth prefix
        yarp.AddRoute("/api/auth/{**catch-all}", auth)
            .WithTransformPathRemovePrefix("/api/auth");

        // Route for Tenancy API - removes /api/tenancy prefix
        yarp.AddRoute("/api/tenancy/{**catch-all}", tenancy)
            .WithTransformPathRemovePrefix("/api/tenancy");

        // Route for Catalog API - removes /api/catalog prefix
        yarp.AddRoute("/api/catalog/{**catch-all}", catalog)
            .WithTransformPathRemovePrefix("/api/catalog");

        // Route for Notifications API - removes /api/notifications prefix
        yarp.AddRoute("/api/notifications/{**catch-all}", notifications)
            .WithTransformPathRemovePrefix("/api/notifications");

        // Optional: Serve static files (your Angular app)
        // yarp.WithStaticFiles("../ShopIt.Angular/dist");
    });


var scalar = builder.AddScalarApiReference(options => options
    .PreferHttpsEndpoint()
    .AllowSelfSignedCertificates());

scalar
    .WithApiReference(auth)
    .WithApiReference(identity)
    .WithApiReference(tenancy)
    .WithApiReference(catalog)
    .WithApiReference(notifications)
    .WithApiReference(gateway)
    .WaitFor(auth)
    .WaitFor(identity)
    .WaitFor(tenancy)
    .WaitFor(catalog)
    .WaitFor(notifications)
    .WaitFor(gateway);

builder.Build().Run();
