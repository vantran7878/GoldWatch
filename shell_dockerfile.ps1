docker run --name goldwatch-postgres -e POSTGRES_USER=goldwatch -e POSTGRES_PASSWORD=goldwatch_dev -e POSTGRES_DB=goldwatch -p 5432:5432 -d postgres

dotnet ef migrations add InitialCreate

dotnet ef database update

# angular create project
cd c:\Users\ASUS\dev\dotnet\GoldWatch
ng new goldwatch-angular --directory frontend/goldwatch-angular --style css --routing true --ssr false
