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

var scalar = builder.AddScalarApiReference(options => options
    .PreferHttpsEndpoint()
    .AllowSelfSignedCertificates());

scalar
    .WithApiReference(auth)
    .WithApiReference(identity);

builder.Build().Run();
