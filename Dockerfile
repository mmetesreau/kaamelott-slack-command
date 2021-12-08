FROM mcr.microsoft.com/dotnet/sdk:6.0
EXPOSE 8080
COPY . .
CMD ["dotnet", "fsi", "src/app.fsx"]