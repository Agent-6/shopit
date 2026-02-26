# Latest .NET base
FROM mcr.microsoft.com/devcontainers/dotnet:dev-10.0-noble

# Add .NET global tools path
ENV PATH=$PATH:/home/vscode/.dotnet:/home/vscode/.dotnet/tools

# Install EF CLI
RUN dotnet tool install --global dotnet-ef

WORKDIR /workspaces
