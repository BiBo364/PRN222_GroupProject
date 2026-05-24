# PRN222_10W_Assignment1

ASP.NET Core MVC app with layered architecture (Repository → Service → Presentation).

## Setup

1. Clone the repository.
2. Copy the example config and set your SQL Server connection:

   ```bash
   copy Assignmet1_Presentation\appsettings.example.json Assignmet1_Presentation\appsettings.json
   ```

3. Edit `Assignmet1_Presentation/appsettings.json` with your local `DefaultConnection`.
4. Run the presentation project:

   ```bash
   dotnet run --project Assignmet1_Presentation
   ```

`appsettings.json` is not committed to Git — use `appsettings.example.json` as a template.
