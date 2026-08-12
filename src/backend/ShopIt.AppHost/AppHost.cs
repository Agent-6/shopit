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

var authDb = authPostgres.AddDatabase("auth-db");
var identityDb = identityPostgres.AddDatabase("identity-db");
var tenancyDb = tenancyPostgres.AddDatabase("tenancy-db");

var kafka = builder.AddKafka("kafka")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var seq = builder.AddSeq("seq")
    .ExcludeFromManifest()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("ACCEPT_EULA", "Y");

var auth = builder.AddProject<Projects.ShopIt_Authentication_API>("auth-api")
    .WithReference(authDb)
    .WithReference(seq)
    .WithReference(kafka)
    .WaitFor(authDb)
    .WaitFor(kafka)
    .WaitFor(seq);

var identity = builder.AddProject<Projects.ShopIt_Identity_API>("identity-api")
    .WithReference(identityDb)
    .WithReference(seq)
    .WithReference(kafka)
    .WaitFor(identityDb)
    .WaitFor(kafka)
    .WaitFor(seq);

auth.WithReference(identity).WaitFor(identity);

var tenancy = builder.AddProject<Projects.ShopIt_Tenancy_API>("tenancy-api")
    .WithReference(tenancyDb)
    .WithReference(seq)
    .WithReference(kafka)
    // Tenancy enforces Identity's permission model: it needs the auth server for
    // client-credentials tokens and the Identity service for permission lookups.
    .WithReference(auth)
    .WithReference(identity)
    .WaitFor(tenancyDb)
    .WaitFor(kafka)
    .WaitFor(seq)
    .WaitFor(auth)
    .WaitFor(identity);

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
    .WithApiReference(gateway)
    .WaitFor(auth)
    .WaitFor(identity)
    .WaitFor(tenancy)
    .WaitFor(gateway);

builder.Build().Run();
