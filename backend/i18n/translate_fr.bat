cd translator\Squidex.Translator

dotnet run translate check-backend ..\..\..\.. -l fr
dotnet run translate check-frontend ..\..\..\.. -l fr

dotnet run translate gen-frontend ..\..\..\.. -l fr
dotnet run translate gen-backend ..\..\..\.. -l fr

