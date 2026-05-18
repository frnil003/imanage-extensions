# FaluHK Samkörning – Kom igång

## Förutsättningar
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (LocalDB ingår i Visual Studio) eller Azure SQL

## Lokal körning

```bash
cd Samkorning

# Återställ paket
dotnet restore

# Skapa databas och kör migrationer
dotnet ef migrations add InitialCreate
dotnet ef database update

# Starta
dotnet run
```

Öppna [https://localhost:5001](https://localhost:5001)

**Admin-inloggning (standard):**
- E-post: `admin@faluhk.se`
- Lösenord: `Admin123!`

Ändra detta i `appsettings.json` eller via User Secrets:
```bash
dotnet user-secrets set "AdminPassword" "DittLösenord!"
```

## Azure-driftsättning

1. Skapa en **Azure SQL Database** och kopiera connection string
2. Skapa en **Azure App Service** (.NET 10)
3. Lägg in connection string i App Service → Configuration → Connection strings:
   - Namn: `DefaultConnection`
   - Värde: din Azure SQL connection string
4. Lägg in admin-uppgifter som App Settings:
   - `AdminEmail` och `AdminPassword`
5. Publicera:
   ```bash
   dotnet publish -c Release
   # eller via Azure DevOps / GitHub Actions
   ```

Databasen migreras automatiskt vid uppstart.

## Konfiguration

| Inställning | Beskrivning | Standard |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Databasanslutning | LocalDB |
| `AdminEmail` | Admin-kontots e-post | admin@faluhk.se |
| `AdminPassword` | Admin-kontots lösenord | Admin123! |
| `MinDriverObligation` | Min. antal körningar per familj | 2 |

## Funktioner

- **Familjer** registrerar sig med namn och e-post
- **Tävlingar** skapas av admin med datum, plats och antal bilar
- **Anmälan** – välj förare (med antal platser) eller passagerare
- **Admin** ser vilka familjer inte uppfyllt körningskravet (≥2 gånger)
- **Tilldelning** – admin kan manuellt eller automatiskt tilldela passagerare till bilar
