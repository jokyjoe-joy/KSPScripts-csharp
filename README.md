# KSPScripts C#

You can find all my Kerbal Space Program C# scripts using kRPC in this repository.

## Usage

```bash
# Clone the repository
git clone https://github.com/jokyjoe-joy/KSPScripts-csharp && cd kspscripts-csharp
# Run the project
# Note: 'restore' & 'build' are run automatically with 'dotnet run'
#dotnet restore # Installing prerequisites
#dotnet build  # Building project
dotnet run
```

## Setting up a new project

```bash
# Create new project and add prerequisites
dotnet new console
dotnet add package KRPC.Client --version 0.4.8
dotnet add package Google.Protobuf --version 3.15.6
```

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.