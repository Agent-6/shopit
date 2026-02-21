var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ShopIt_Identity_API>("Identity-API");

builder.Build().Run();
