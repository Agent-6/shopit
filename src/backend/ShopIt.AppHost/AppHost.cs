using Aspire.Hosting.Yarp.Transforms;
using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var authDb = postgres.AddDatabase("auth-db");
var identityDb = postgres.AddDatabase("identity-db");

var seq = builder.AddSeq("seq")
    .ExcludeFromManifest()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("ACCEPT_EULA", "Y");

var auth = builder.AddProject<Projects.ShopIt_Authentication_API>("auth-api")
    .WithReference(authDb)
    .WithReference(seq)
    .WaitFor(authDb)
    .WaitFor(seq);

var identity = builder.AddProject<Projects.ShopIt_Identity_API>("identity-api")
    .WithReference(identityDb)
    .WithReference(seq)
    .WaitFor(identityDb)
    .WaitFor(seq);

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

        // Optional: Serve static files (your Angular app)
        // yarp.WithStaticFiles("../ShopIt.Angular/dist");
    });


var scalar = builder.AddScalarApiReference(options => options
    .PreferHttpsEndpoint()
    .AllowSelfSignedCertificates());

scalar
    .WithApiReference(auth)
    .WithApiReference(identity)
    .WithApiReference(gateway)
    .WaitFor(auth)
    .WaitFor(identity)
    .WaitFor(gateway);

builder.Build().Run();
