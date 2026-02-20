# Latest .NET base
FROM mcr.microsoft.com/devcontainers/dotnet:10.0

# Add .NET global tools path
ENV PATH=$PATH:/home/vscode/.dotnet:/home/vscode/.dotnet/tools

# Install EF CLI
RUN dotnet tool install --global dotnet-ef

WORKDIR /workspaces
