# Zadání Sklad (WPF, .NET 9) – README

Tento dokument shrnuje **strukturu projektu** podle architektury **MVVM**, obsahuje ukázkový kód pro **Helper `RelayCommand`**, doporučení pro **Converters** a **podrobný výklad MVVM** včetně malého příkladu *Model → ViewModel → View*.

---

## Struktura projektu (MVVM)

```text
Sklad/
├─ Converters/                  # Převodníky pro XAML (IValueConverter)
│  └─ EnumDescriptionConverter.cs
│
├─ Globals/                     # Sdílené služby a konfigurace (UI-agnostické)
│  └─ Globals.cs
│
├─ Helpers/                     # Pomocné utility (bez závislosti na konkrétním View)
│  └─ RelayCommand.cs
│
├─ Images/                      # Obrázky/ikony (Build Action: Resource)
│  ├─ Icons/
│  └─ MainMenu/
│
├─ ViewModels/                  # Logika obrazovek (INotifyPropertyChanged, ICommand)
│  └─ MainWindowViewModel.cs
│
└─ Views/                       # Vizuální vrstva (XAML + code-behind)
   ├─ MainWindow.xaml
   └─ MainWindow.xaml.cs
```

### Doporučené namespaces
```
Sklad.Converters
Sklad.Globals
Sklad.Helpers
Sklad.ViewModels
Sklad.Views
```

---

## Helpers: `RelayCommand`

`RelayCommand` je obecná implementace `ICommand`, která umožňuje ve ViewModelu snadno vystavit akce pro tlačítka a jiné prvky UI bez psaní repetitivního kódu.

> Ulož do **`Helpers/RelayCommand.cs`**

```csharp
using System;
using System.Windows.Input;

namespace Sklad.Helpers
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add    { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
```

### Jak `RelayCommand` použít ve ViewModelu

```csharp
using System.Windows.Input;
using Sklad.Helpers;

namespace Sklad.ViewModels
{
    public class MainWindowViewModel : BaseVM // BaseVM: INotifyPropertyChanged
    {
        public ICommand OpenOverviewCommand { get; }

        public MainWindowViewModel()
        {
            OpenOverviewCommand = new RelayCommand(_ => OpenOverview());
        }

        private void OpenOverview()
        {
            // zde čistá VM logika, např. nastavení stavů, volání služeb, navigační event apod.
        }
    }
}
```

A ve **XAML**:
```xml
<Button Content="Otevřít přehled" Command="{Binding OpenOverviewCommand}" />
```

---

## Converters: příklad registrace v `App.xaml`

```xml
<Application x:Class="Sklad.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:converters="clr-namespace:Sklad.Converters">
  <Application.Resources>
    <converters:EnumDescriptionConverter x:Key="EnumToStringConverter" />
  </Application.Resources>
</Application>
```

Použití v XAML:
```xml
<TextBlock Text="{Binding StavObjednavky, Converter={StaticResource EnumToStringConverter}}" />
```

---

## MVVM – důkladné vysvětlení

**MVVM (Model–View–ViewModel)** je architektonický vzor pro WPF (a další XAML technologie), který **odděluje** uživatelské rozhraní (View) od aplikační logiky a stavu (ViewModel) a od datového/doménového modelu (Model).

### Role vrstev

- **Model**
  - Reprezentuje *doménová data a pravidla* (např. `Material`, `MernaJednotka`).
  - Neřeší UI – žádné `MessageBox`, žádné `DependencyObject`.
  - Obsahuje validace a datové kontrakty; často generován/přichází z EF Core (`DbContext` + entity).

- **View (XAML + .xaml.cs)**
  - *Jak to vypadá a reaguje.* Obsahuje deklarativní UI a vizuální chování.
  - Může nastavit `DataContext`, reagovat na čistě vizuální události (fokus, animace).
  - Nemá obsahovat business logiku ani přímou práci s databází.

- **ViewModel**
  - *Stav a chování pro View.*
  - Implementuje `INotifyPropertyChanged` (příp. Fody – `[AddINotifyPropertyChangedInterface]`), vystavuje **vlastnosti** pro binding a **ICommand** pro akce.
  - Je UI-agnostický (žádné typy z `System.Windows.*`), takže lze snadno testovat unit testy.
  - Zpravidla používá služby/repositáře pro přístup k datům (např. přes `Globals`/DI).

### Databinding a ICommand

- **Binding** zajišťuje, že UI *pozoruje* ViewModel.
  - Změna ve ViewModelu (notifikace `PropertyChanged`) aktualizuje UI bez zásahu code-behind.
- **Commandy** (`ICommand`) vystavují akce (např. uložit, načíst, otevřít okno) bez nutnosti psát click handlery ve View.

### Výhody MVVM

- **Testovatelnost:** ViewModel je bez UI závislostí.
- **Opakovatelnost** a **znovupoužitelnost**: stejné VM lze zobrazit různými View.
- **Čistota kódu:** koncentrovaná logika, méně code-behind.
- **Paralelní vývoj:** UX/UI designer pracuje na XAML, vývojář na VM/services.

### Anti-patterns (na co pozor)

- **Těžký code-behind:** business logika v `*.xaml.cs` → přesuň do VM.
- **UI typy ve ViewModelu:** např. `Brush`, `MessageBox` → místo toho hodnoty a konvertory/eventy.
- **Přímý EF v View:** přístup k databázi patří do služby/VM, ne do XAML/code-behind.

---

## Mini příklad: Model → ViewModel → View

### Model (zjednodušeně)
```csharp
public class Material
{
    public int Id { get; set; }
    public string Nazev { get; set; } = string.Empty;
    public string Jednotka { get; set; } = "ks";
}
```

### ViewModel
```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Sklad.Helpers;

public class MaterialyViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Material> Materialy { get; } = new();
    public ICommand PridatCommand { get; }
    private string _novyNazev = string.Empty;

    public string NovyNazev
    {
        get => _novyNazev;
        set { _novyNazev = value; OnPropertyChanged(); }
    }

    public MaterialyViewModel()
    {
        PridatCommand = new RelayCommand(_ => Pridat(), _ => !string.IsNullOrWhiteSpace(NovyNazev));
    }

    private void Pridat()
    {
        Materialy.Add(new Material { Id = Materialy.Count + 1, Nazev = NovyNazev, Jednotka = "ks" });
        NovyNazev = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```

### View (XAML)
```xml
<Window x:Class="Sklad.Views.MaterialyWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace=Sklad.ViewModels"
        Title="Materiály" Height="400" Width="600">
  <Window.DataContext>
    <vm:MaterialyViewModel/>
  </Window.DataContext>

  <DockPanel Margin="12">
    <StackPanel Orientation="Horizontal" DockPanel.Dock="Top" Margin="0,0,0,8">
      <TextBox Width="200" Text="{Binding NovyNazev, UpdateSourceTrigger=PropertyChanged}"/>
      <Button Content="Přidat" Margin="8,0,0,0" Command="{Binding PridatCommand}"/>
    </StackPanel>

    <DataGrid ItemsSource="{Binding Materialy}" AutoGenerateColumns="False">
      <DataGrid.Columns>
        <DataGridTextColumn Header="ID" Binding="{Binding Id}" Width="Auto"/>
        <DataGridTextColumn Header="Název" Binding="{Binding Nazev}" Width="*"/>
        <DataGridTextColumn Header="Jednotka" Binding="{Binding Jednotka}" Width="Auto"/>
      </DataGrid.Columns>
    </DataGrid>
  </DockPanel>
</Window>
```

Všimni si:
- View neobsahuje žádnou business logiku – jen XAML.
- Chování je řízené z ViewModelu (`PridatCommand`, `NovyNazev`, `Materialy`).

---

## Checklist rychlé kontroly

- [ ] `Views/` obsahuje XAML + code-behind a pouze UI logiku.
- [ ] `ViewModels/` obsahuje stav a `ICommand`, bez UI typů.
- [ ] `Helpers/RelayCommand.cs` funguje a je použit ve ViewModelu.
- [ ] `Converters/` registrované v `App.xaml`.
- [ ] Modely a data nejsou přímo ve View – přístup přes VM/služby.

---

© 2025 – Sklad (WPF, .NET 9), MVVM skeleton a best practices.





# Sklad – WPF **Application** (.NET 9) + Entity Framework Core 9
> **Cíl**: podle tohoto README krok‑za‑krokem klikací cestou vytvoříte školní WPF *Application* projekt (pozor: **ne „WPF App“**), připojíte EF Core 9 (SQL Server LocalDB), vytvoříte migrace, osadíte data, zobrazíte je v UI a ověříte je přímo ve Visual Studio. Součástí je i vysvětlení všech důležitých řádků kódu a pojmů (migrace, relace), plus dole sada **zadání + řešení** s bohatě okomentovaným kódem.

---

## 0) Předpoklady (instalace a co zkontrolovat)
1. **Visual Studio 2022** (aktuální verze) s pracovním zatížením **„Desktop development with .NET“**.  
   *Zkontrolujete v* **Visual Studio Installer → Modify → Desktop development with .NET** (musí být zaškrtnuto).  
   Toto nainstaluje šablony WPF i SQL Server LocalDB.
2. **.NET SDK 9** – Visual Studio jej nainstaluje automaticky s výše uvedeným workloadem. (V případě starší instalace můžete stáhnout ručně z oficiálních stránek.)
3. **SQL Server LocalDB** (součást Visual Studia). Budeme jej využívat pro lokální databázi `SkladDb`.
4. **Jazyk C#** – vše je psané v C# 12/13 (dle .NET 9).

> **Pozor na šablonu:** podle pokynů v zadání **zakládáme „WPF Application“** (často lokalizované jako „WPF aplikace“), **ne „WPF App“**. V některých instalacích se názvy mohou lišit; řiďte se proto primárně tímto README a volbou cílového frameworku **.NET 9 (Windows)**.  

---

## 1) Vytvoření projektu (klikací postup)
1. **File → New → Project…**
2. Do vyhledávání napište **WPF** a zvolte **WPF Application** (nikoliv „WPF App“).  
3. **Project name:** `Sklad` (můžete nechat i jiný, README se ale na `Sklad` odkazuje).  
   **Location:** libovolně, ideálně *„Place solution and project in the same directory“* vypněte (nebo ponechte dle zvyku).  
4. **Framework:** vyberte **.NET 9 (Windows)**.  
5. Potvrďte **Create**.

> Pokud by šablona nenabízela přesně .NET 9 Windows, po vytvoření projektu to opravíme ručně v *.csproj* (viz níže).

---

## 2) Kontrola/úprava projektu (*.csproj*)
Otevřete soubor **`Sklad.csproj`** a zkontrolujte, případně upravte:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- Cílový framework: WPF vyžaduje windows variantu TFM. -->
    <TargetFramework>net9.0-windows</TargetFramework>

    <!-- Zapne WPF build tasks a XAML kompilaci. -->
    <UseWPF>true</UseWPF>

    <!-- (Volitelné) Povolit nullable reference types – pomáhá předcházet chybám. -->
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```
**Vysvětlení:**
- `TargetFramework` je **net9.0-windows** → říká kompilátoru i nástrojům, že projekt je WPF pro Windows na .NET 9.
- `UseWPF` musí být `true`, jinak WPF okna/zdroje nepůjdou sestavit.
- `Nullable` je doporučené – upozorní na potenciální `null` problémy už při kompilaci.

---

## 3) Instalace NuGet balíčků (klikací, přes GUI)
Klikněte **Project → Manage NuGet Packages…** a na kartě **Browse** nainstalujte do projektu tyto balíčky (verze **9.x** – zvolte aktuální 9.*):
- **Microsoft.EntityFrameworkCore** – základ EF Core (ORM).
- **Microsoft.EntityFrameworkCore.SqlServer** – poskytovatel pro SQL Server (LocalDB).
- **Microsoft.EntityFrameworkCore.Tools** – nástroje pro migrace (Add‑Migration, Update‑Database aj.).
- **Microsoft.EntityFrameworkCore.Design** – design‑time podpůrné typy (pomáhá nástrojům).
- **CommunityToolkit.Mvvm** *(volitelné, ale doporučené)* – MVVM toolkit pro jednodušší ViewModely a příkazy.

> **Proč/na co to je a kde to použijeme:**
> - `Microsoft.EntityFrameworkCore` – umožní psát dotazy LINQ, trackovat změny a mapovat C# třídy na tabulky.
> - `Microsoft.EntityFrameworkCore.SqlServer` – přidá `UseSqlServer(...)` a SQL Server specifika (LocalDB i plný SQL Server).
> - `Microsoft.EntityFrameworkCore.Tools` – umožní **migrace** (generování a pouštění SQL skriptů ze schématu modelu).
> - `Microsoft.EntityFrameworkCore.Design` – obsahuje design‑time rozhraní (např. `IDesignTimeDbContextFactory`), která nástroje využijí při generování migrací.
> - `CommunityToolkit.Mvvm` – rychlé MVVM: atributy `[ObservableProperty]`, `[RelayCommand]`, implementace `INotifyPropertyChanged` atd.

---

## 4) Přidání connection stringu
Budeme používat **SQL Server LocalDB** s názvem databáze `SkladDb`.

### Varianta A – přímo v kódu (nejjednodušší pro školní projekty)
Nebudeme řešit konfigurační soubory – vše zůstane lokální a přehledné.

> Connection string (použijeme za chvíli v `DbContext`):
```
Server=(localdb)\MSSQLLocalDB;Database=SkladDb;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True
```

### Varianta B – (volitelné) `appsettings.json`
Pokročilejší přístup: přidejte do projektu nový **JSON File** `appsettings.json`, nastavte **Copy to Output Directory → Copy always** a vložte:
```json
{
  "ConnectionStrings": {
    "SkladDb": "Server=(localdb)\\MSSQLLocalDB;Database=SkladDb;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
  }
}
```
Poté byste museli číst konfiguraci (např. přes `Microsoft.Extensions.Configuration`) – pro minimalismus to zde **nebudeme** dělat, ale níže ukážeme design‑time factory.

---

## 5) Doménový model (třídy entit)
Vytvořte složku **`Data/Entities`** a přidejte tyto soubory:

### 5.1 `Category.cs`
```csharp
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sklad.Data.Entities
{
    // Reprezentuje kategorii zboží (např. "Elektronika").
    public class Category
    {
        // Primární klíč. EF Core podle konvence rozpozná "Id" jako PK (int identity).
        public int Id { get; set; }

        // Název kategorie – [Required] zajistí, že v DB nesmí být NULL.
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Navigační vlastnost 1:N – jedna kategorie má mnoho produktů.
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
```

### 5.2 `Product.cs`
```csharp
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sklad.Data.Entities
{
    // Reprezentuje produkt (položku skladové karty).
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // Cizí klíč na Category (FK sloupec v tabulce Products).
        public int CategoryId { get; set; }

        // Navigace na Category – EF sestaví vztah 1:N (Category -> Products).
        public Category? Category { get; set; }

        // Navigace 1:1 na detail produktu (tabulka ProductDetails).
        public ProductDetail? Detail { get; set; }

        // Navigace M:N na dodavatele přes spojovací entitu ProductSupplier.
        public ICollection<ProductSupplier> ProductSuppliers { get; set; } = new List<ProductSupplier>();

        // Navigace 1:N na položky objednávky (Product má mnoho OrderItems).
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
```

### 5.3 `ProductDetail.cs` (1:1 s Product)
```csharp
using System.ComponentModel.DataAnnotations;

namespace Sklad.Data.Entities
{
    // Detail produktu (jedna řádka na jeden Product).
    public class ProductDetail
    {
        // PK = zároveň FK na Product (1:1 sdílený klíč).
        [Key]
        public int ProductId { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public double WeightKg { get; set; }

        // Navigace zpět na Product.
        public Product? Product { get; set; }
    }
}
```

### 5.4 `Supplier.cs` (Dodavatel)
```csharp
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sklad.Data.Entities
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? City { get; set; }

        // Navigace M:N – spojovací entita.
        public ICollection<ProductSupplier> ProductSuppliers { get; set; } = new List<ProductSupplier>();
    }
}
```

### 5.5 `ProductSupplier.cs` (spojovací tabulka M:N)
```csharp
namespace Sklad.Data.Entities
{
    // Explicitní spojovací entita pro vztah Product <-> Supplier (M:N).
    public class ProductSupplier
    {
        // Složený primární klíč (ProductId, SupplierId) nastavíme ve Fluent API.
        public int ProductId { get; set; }
        public int SupplierId { get; set; }

        // Navigace na koncové entity.
        public Product? Product { get; set; }
        public Supplier? Supplier { get; set; }
    }
}
```

### 5.6 `Order.cs` (hlavička objednávky)
```csharp
using System;
using System.Collections.Generic;

namespace Sklad.Data.Entities
{
    public class Order
    {
        public int Id { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string? Customer { get; set; }

        // 1:N – objednávka má mnoho položek.
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
```

### 5.7 `OrderItem.cs` (položka objednávky, klíč (OrderId, ProductId))
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sklad.Data.Entities
{
    public class OrderItem
    {
        // Složený klíč (OrderId, ProductId) nastavíme ve Fluent API.
        public int OrderId { get; set; }
        public int ProductId { get; set; }

        // Kolik kusů daného produktu je v objednávce.
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        // Navigace
        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}
```

---

## 6) EF Core DbContext + mapování relací
Složka **`Data`** → soubor **`SkladContext.cs`**:

```csharp
using Microsoft.EntityFrameworkCore;
using Sklad.Data.Entities;

namespace Sklad.Data
{
    // DbContext definuje "DB pohled" na naše entity a pravidla mapování.
    public class SkladContext : DbContext
    {
        // Konstruktor pro běh aplikace – předáváme options (např. připojení).
        public SkladContext(DbContextOptions<SkladContext> options) : base(options) { }

        // Parametrless konstruktor pro jednoduché scénáře a návrhové nástroje.
        public SkladContext() { }

        // DbSety = tabulky
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductDetail> ProductDetails => Set<ProductDetail>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<ProductSupplier> ProductSuppliers => Set<ProductSupplier>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        // Pokud aplikace nepředá options (např. při prostém new SkladContext()),
        // nastavíme je zde – jednoduché lokální připojení na LocalDB.
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    "Server=(localdb)\\MSSQLLocalDB;Database=SkladDb;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True");
            }
        }

        // Jemné nastavení relací + seed dat (HasData).
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1:N Category -> Products
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // smazání kategorie neodstraní produkty

            // 1:1 Product -> ProductDetail (sdílený klíč)
            modelBuilder.Entity<ProductDetail>()
                .HasOne(d => d.Product)
                .WithOne(p => p.Detail)
                .HasForeignKey<ProductDetail>(d => d.ProductId);

            // M:N přes explicitní entitu ProductSupplier
            modelBuilder.Entity<ProductSupplier>()
                .HasKey(ps => new { ps.ProductId, ps.SupplierId });

            modelBuilder.Entity<ProductSupplier>()
                .HasOne(ps => ps.Product)
                .WithMany(p => p.ProductSuppliers)
                .HasForeignKey(ps => ps.ProductId);

            modelBuilder.Entity<ProductSupplier>()
                .HasOne(ps => ps.Supplier)
                .WithMany(s => s.ProductSuppliers)
                .HasForeignKey(ps => ps.SupplierId);

            // 1:N Order -> OrderItems + složený klíč
            modelBuilder.Entity<OrderItem>()
                .HasKey(oi => new { oi.OrderId, oi.ProductId });

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId);

            // ---------- SEED DATA (pro první naplnění tabulek) ----------

            // Kategorie
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Elektronika" },
                new Category { Id = 2, Name = "Kancelář" }
            );

            // Dodavatelé
            modelBuilder.Entity<Supplier>().HasData(
                new Supplier { Id = 1, Name = "Tech s.r.o.", City = "Praha" },
                new Supplier { Id = 2, Name = "Office a.s.", City = "Brno" }
            );

            // Produkty
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Notebook X100", CategoryId = 1 },
                new Product { Id = 2, Name = "Myš USB",        CategoryId = 1 },
                new Product { Id = 3, Name = "Sešit A4",       CategoryId = 2 }
            );

            // Detaily (1:1 – PK=FK)
            modelBuilder.Entity<ProductDetail>().HasData(
                new ProductDetail { ProductId = 1, Description = "14\" notebook, 16GB RAM", WeightKg = 1.35 },
                new ProductDetail { ProductId = 2, Description = "Optická myš, 1600 DPI",    WeightKg = 0.08 },
                new ProductDetail { ProductId = 3, Description = "Čtverečkovaný, 80 listů",  WeightKg = 0.45 }
            );

            // M:N vazby přes join entitu
            modelBuilder.Entity<ProductSupplier>().HasData(
                new ProductSupplier { ProductId = 1, SupplierId = 1 },
                new ProductSupplier { ProductId = 2, SupplierId = 1 },
                new ProductSupplier { ProductId = 3, SupplierId = 2 }
            );
        }
    }
}
```

**Komentář k relacím:**
- **1:N** `Category` → `Product` (každý produkt má právě jednu kategorii, kategorie má mnoho produktů).
- **1:1** `Product` ↔ `ProductDetail` (sdílený klíč – sloupec `ProductDetails.ProductId` je zároveň PK i FK).
- **M:N** `Product` ↔ `Supplier` přes **explicitní spojovací entitu** `ProductSupplier` (máme nad ní plnou kontrolu).
- **1:N** `Order` → `OrderItem` a současně `Product` → `OrderItem`; `OrderItem` má **složený klíč** `(OrderId, ProductId)`.

> **Proč dáváme seed (`HasData`) do `OnModelCreating`:** při **migraci** se vytvoří SQL INSERTy a data se objeví hned po `Update-Database`.

---

## 7) Design‑time továrna pro migrace
U WPF často nepoužíváte „webový host“ ani DI při generování migrací. Nástroje EF Core proto někdy nevědí, **jak vytvořit `DbContext`**. Nejspolehlivější je přidat **`IDesignTimeDbContextFactory`**.

Složka **`Data`** → **`SkladContextFactory.cs`**:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sklad.Data
{
    // Továrna, kterou si EF Core nástroje vyžádají v DESIGN-TIME (při Add-Migration).
    // Díky ní nástroje přesně vědí, jak SkladContext sestavit (včetně connection stringu).
    public class SkladContextFactory : IDesignTimeDbContextFactory<SkladContext>
    {
        public SkladContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<SkladContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=SkladDb;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True")
                .Options;

            return new SkladContext(options);
        }
    }
}
```

---

## 8) První migrace (jediná „neklikací“ část)
1. **Tools → NuGet Package Manager → Package Manager Console** (PMC).
2. V rozbalovačce **Default project** vyberte projekt **Sklad**.
3. Spusťte postupně příkazy:
   ```powershell
   Add-Migration InitialCreate
   Update-Database
   ```
   - `Add-Migration` vyrobí migrační třídu s SQL změnami podle modelu.
   - `Update-Database` tyto změny **aplikuje** do LocalDB a vytvoří DB `SkladDb` s tabulkami i seed daty.

> Alternativa CLI (kdybyste používali `dotnet ef`):  
> `dotnet ef migrations add InitialCreate` → `dotnet ef database update` (ve složce projektu).

---

## 9) Ověření v databázi **přímo ve Visual Studio**
1. Otevřete **View → SQL Server Object Explorer** (nebo **Server Explorer**).
2. Rozbalte **(localdb)\MSSQLLocalDB** → **Databases**.
3. Najděte **`SkladDb`** → rozbalte **Tables**.
4. Na tabulce (např. `dbo.Products`) **pravým** → **View Data** (zobrazí prvních 200 řádků).  
   Měli byste vidět produkty a seedovaná data.
5. (Volitelně) **New Query** a spusťte např.:
   ```sql
   SELECT p.Id, p.Name, c.Name AS Category
   FROM dbo.Products p
   JOIN dbo.Categories c ON c.Id = p.CategoryId
   ORDER BY p.Id;
   ```

---

## 10) Zobrazení dat ve WPF okně
V **`MainWindow.xaml`** nahraďte obsah okna jednoduchou mřížkou s tlačítky:

```xml
<Window x:Class="Sklad.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Sklad" Height="450" Width="800">
    <DockPanel Margin="12">
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,8">
            <Button x:Name="btnRefresh" Content="Načíst" Width="100" Margin="0,0,8,0" Click="Refresh_Click"/>
            <Button x:Name="btnAdd" Content="Přidat test produkt" Width="180" Click="Add_Click"/>
        </StackPanel>

        <DataGrid x:Name="grid"
                  AutoGenerateColumns="False"
                  IsReadOnly="True">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Id" Binding="{Binding Id}" Width="60"/>
                <DataGridTextColumn Header="Název" Binding="{Binding Name}" Width="*"/>
                <DataGridTextColumn Header="Kategorie" Binding="{Binding Category.Name}" Width="200"/>
            </DataGrid.Columns>
        </DataGrid>
    </DockPanel>
</Window>
```

**`MainWindow.xaml.cs`** – jednoduchá „code‑behind“ verze pro přehlednost (MVVM můžete doplnit později):
```csharp
using Microsoft.EntityFrameworkCore;
using Sklad.Data;
using Sklad.Data.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Sklad
{
    public partial class MainWindow : Window
    {
        // Pro ukázku používáme dlouhožijící context v rámci okna (školní/demo přístup).
        private readonly SkladContext _db = new();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                // Při prvním spuštění zajistí vytvoření DB a aplikaci všech migrací.
                await _db.Database.MigrateAsync();

                await LoadAsync();
            };
        }

        private async Task LoadAsync()
        {
            // Načte produkty vč. navázané kategorie.
            var data = await _db.Products
                                .Include(p => p.Category)
                                .OrderBy(p => p.Id)
                                .ToListAsync();

            grid.ItemsSource = data;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadAsync();
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            // Přidáme testovací položku do kategorie 1 (Elektronika).
            var p = new Product { Name = "Test produkt", CategoryId = 1 };
            _db.Products.Add(p);
            await _db.SaveChangesAsync();
            await LoadAsync();
        }
    }
}
```

**Co se tu děje (řádek po řádku, stručně):**
- `SkladContext _db = new();` – vytvoří kontext s defaultním `OnConfiguring` (LocalDB).
- `Database.MigrateAsync()` – pokud chybí DB/tabulky, **vytvoří je a aplikuje všechny migrace**.
- `Include(p => p.Category)` – dotáhne navázanou entitu (JOIN v SQL).
- Tlačítka volají metody, které vloží data a znovu načtou mřížku.

> **Poznámka:** pro větší aplikaci použijte MVVM (např. `CommunityToolkit.Mvvm`) a DI. Do školního minima je code‑behind přehlednější.

---

## 11) Co jsou **migrace**, proč a jak je používáme
- **Migrace** jsou verzované „balíčky změn schématu“ databáze generované EF Core z vašeho **modelu** (třídy + Fluent API).  
  Každá migrace má metody **`Up()`** (aplikuj změny) a **`Down()`** (vrat je zpět).
- Při `Add-Migration` EF porovná aktuální **model** s posledním **snímkem** (ModelSnapshot) a vytvoří C# kód, který po překladu vygeneruje SQL.
- `Update-Database` aplikuje migrace na DB a zapíše je do tabulky `__EFMigrationsHistory`.

**Typické příkazy (PMC):**
```powershell
# Vygeneruj migraci podle aktuálního modelu
Add-Migration InitialCreate

# Aplikuj všechny neaplikované migrace na DB
Update-Database

# Vrátit poslední migraci (pokud nebyla aplikovaná na DB)
Remove-Migration

# Přegenerovat SQL skript pro nasazení
Script-Migration

# (Pokročilé) Vrátit DB na konkrétní migraci
Update-Database NecoPredtim
```
**Kdy dělat migraci:** kdykoli **změníte model** (přidáte/odeberete/změníte vlastnost, entitu nebo relaci).

---

## 12) Jaké **relace** známe a které tu používáme
- **1:N (one‑to‑many)** – např. `Category` → `Product`. Implementace: `HasOne(...).WithMany(...)` + cizí klíč.
- **1:1 (one‑to‑one)** – `Product` ↔ `ProductDetail` (sdílený klíč). Implementace: `HasOne(...).WithOne(...)` + `HasForeignKey<T>(...)`.
- **M:N (many‑to‑many)** – `Product` ↔ `Supplier`. Použili jsme **explicitní join** `ProductSupplier` se složeným klíčem `(ProductId, SupplierId)`.

Každou relaci jsme nadefinovali **Fluent API** v `OnModelCreating` a okomentovali výše.

---

## 13) Časté problémy a jejich řešení
- **Nástroje hlásí „Unable to create DbContext“ při Add‑Migration** → ujistěte se, že existuje `SkladContextFactory` (viz kapitola 7).  
- **Nenajdu `SkladDb` v SQL Server Object Explorer** → zkontrolujte, že jste po `Add-Migration` pustili **`Update-Database`** a že používáte instanci **(localdb)\MSSQLLocalDB**.
- **Chybí tabulky** → v aplikaci běžně voláme `Database.MigrateAsync()`, což je také vytvoří při prvním spuštění.

---

# ✍️ Zadání + řešení (bod po bodu)

Níže najdete samostatná zadání, každé **se vzorovým řešením** a **spoustou komentářů**. Doporučuji postupovat **sekvenčně** – každé navazuje na předchozí a demonstruje práci s **migracemi**.

---

## Zadání 1: Založte projekt a vytvořte model „Kategorie–Produkt“, udělejte první migraci, data načtěte v okně.
### Postup (co student udělá)
1. Vytvořte **WPF Application** pro **.NET 9 (Windows)** s názvem `Sklad`.
2. Přidejte NuGet balíčky z kapitoly 3.
3. Přidejte entity **`Category`** a **`Product`** (viz kapitola 5.1 a 5.2).
4. Nastavte relaci 1:N v `OnModelCreating` (kapitola 6).
5. Přidejte **seed** kategorií a pár produktů.
6. Vytvořte migraci `InitialCreate` a spusťte `Update-Database`.
7. V `MainWindow` zobrazte produkty v `DataGrid` (viz kapitola 10) a ověřte ve VS, že jsou v DB.

### Řešení (kontrolní výpisy a vysvětlení)
- **Migrace** v projektu → složka **Migrations** (soubor `*_InitialCreate.cs` + `*_ModelSnapshot.cs`).  
  Otevřete migraci a všimněte si metod `Up()` a `Down()` – jsou to instrukce, které EF převede na SQL.
- **Ověření DB**: v SQL Server Object Explorer najdete `SkladDb → Tables → dbo.Products`, **View Data** – uvidíte řádky seedovaných produktů.
- **Kód** je již v kapitolách 5–10. Při problémech spusťte aplikaci – `Database.MigrateAsync()` DB také připraví.

---

## Zadání 2: Přidejte entitu `Supplier` a vztah **M:N** mezi `Product` a `Supplier`. Zobrazte dodavatele vybraného produktu.
### Postup
1. Přidejte `Supplier` a `ProductSupplier` podle kapitol 5.4–5.5.
2. Nakonfigurujte **M:N** ve Fluent API (kap. 6) a přidejte `HasData` pro vazby (min. 1–2 dodavatele).
3. `Add-Migration AddSuppliers` → `Update-Database`.
4. Do UI přidejte **TextBlock** (nebo nový sloupec DataGridu) zobrazující dodavatele prvního/označeného produktu.

### Ukázkové změny v UI (sloupec se seznamem dodavatelů)
```xml
<DataGridTextColumn Header="Dodavatelé"
                    Binding="{Binding ProductSuppliers[0].Supplier.Name}"
                    Width="200"/>
```
> Pozn.: pro jednoduchost bereme prvního dodavatele; v praxi byste použili `ItemsControl` nebo šablonu s `ItemsSource`.

### Řešení – kód (Entity + Fluent API)
Již obsaženo výše (kap. 5.4–5.5 a kap. 6). **Klíčové je**, že `ProductSupplier` má složený klíč a `HasData` doplní propojení (viz kapitola 6 – blok *SEED DATA*).  
Po migraci ověřte tabulku **`dbo.ProductSuppliers`**: měla by obsahovat dvojice `(ProductId, SupplierId)`.

---

## Zadání 3: Přidejte `Order` a `OrderItem` se složeným klíčem, vytvořte objednávku, vložte 2 položky a zobrazte je.
### Postup
1. Přidejte entity `Order` a `OrderItem` (kap. 5.6–5.7).
2. Nastavte klíč `HasKey(oi => new { oi.OrderId, oi.ProductId })` (kap. 6).
3. `Add-Migration Orders` → `Update-Database`.
4. Do okna přidejte tlačítko **„Vytvořit test objednávku“**.

### Řešení – doplnění do `MainWindow.xaml` a `.cs`
**UI – přidáme tlačítko:**
```xml
<Button Content="Vytvořit test objednávku" Width="220" Margin="8,0,0,0" Click="CreateOrder_Click"/>
```
**Code-behind:**
```csharp
private async void CreateOrder_Click(object sender, RoutedEventArgs e)
{
    // Vybereme dva existující produkty (Id 1 a 2).
    var p1 = await _db.Products.FindAsync(1);
    var p2 = await _db.Products.FindAsync(2);
    if (p1 is null || p2 is null) { MessageBox.Show("Chybí seed produkty."); return; }

    var order = new Order { Customer = "Školní zákazník" };
    _db.Orders.Add(order);

    _db.OrderItems.Add(new OrderItem { Order = order, Product = p1, Quantity = 1 });
    _db.OrderItems.Add(new OrderItem { Order = order, Product = p2, Quantity = 3 });

    await _db.SaveChangesAsync();
    MessageBox.Show($"Objednávka {order.Id} vytvořena.");
}
```
**Komentáře kód řádek‑po‑řádku:**
- `FindAsync(1)` – rychlé načtení podle klíče.
- Vytvoříme `Order` (hlavičku) a přidáme dvě položky `OrderItem` s odkazem na vybrané produkty.
- `SaveChangesAsync()` uloží *vše* v jedné transakci; EF sám správně vyplní cizí klíče.

Ověřte v DB tabulky `Orders` a `OrderItems` (View Data).

---

## Zadání 4: Přejmenujte vlastnost `Product.Name` na `Product.Title` tak, aby se **neztratila data**.
### Postup
1. Změňte v `Product` vlastnost z `Name` na `Title` a upravte vazby/Bindingy v UI.
2. `Add-Migration RenameProductNameToTitle`.
3. Vygenerovanou migraci **zkontrolujte** – EF obvykle rozpozná rename; pokud ne, upravte ručně:
   ```csharp
   migrationBuilder.RenameColumn(
       name: "Name",
       table: "Products",
       newName: "Title");
   ```
4. `Update-Database` – zkontrolujte, že v DB **zůstala** původní data ve sloupci (už přejmenovaném).

### Řešení – úprava UI bindingu
```xml
<DataGridTextColumn Header="Název" Binding="{Binding Title}" Width="*"/>
```
**Komentář:** Taková změna je *typický důvod* pro migraci – chceme **evoluovat schéma beze ztráty dat**.

---

## Zadání 5: Přidejte entitu `ProductDetail` (1:1), doplňte migraci a zobrazte popis v UI.
### Postup
1. Přidejte `ProductDetail` (kap. 5.3) a relaci 1:1 v `OnModelCreating` (kap. 6).
2. Doporučeno osadit `HasData` pro existující produkty (viz kap. 6).
3. `Add-Migration ProductDetail` → `Update-Database`.
4. V UI přidejte sloupec s `Detail.Description`.

### Řešení – úprava UI
```xml
<DataGridTextColumn Header="Popis" Binding="{Binding Detail.Description}" Width="300"/>
```
**Komentář:** Relace 1:1 se nejčastěji implementuje **sdíleným klíčem** (PK v „detailové“ tabulce je zároveň FK na hlavní tabulku).

---

# Dodatky

## A) Minimalistická verze MVVM (volitelné)
Pokud si chcete osahat `CommunityToolkit.Mvvm`, vytvořte `MainViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Sklad.Data;
using Sklad.Data.Entities;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Sklad
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SkladContext _db = new();

        [ObservableProperty]
        private ObservableCollection<Product> _products = new();

        [RelayCommand]
        private async Task LoadAsync()
        {
            var data = await _db.Products.Include(p => p.Category).ToListAsync();
            Products = new ObservableCollection<Product>(data);
        }

        [RelayCommand]
        private async Task AddAsync()
        {
            _db.Products.Add(new Product { Name = "MVVM produkt", CategoryId = 1 });
            await _db.SaveChangesAsync();
            await LoadAsync();
        }
    }
}
```
`MainWindow.xaml` pak může mít:
```xml
<Window ...
        xmlns:vm="clr-namespace:Sklad">
    <Window.DataContext>
        <vm:MainViewModel/>
    </Window.DataContext>
    <!-- Binding na příkazy: -->
    <Button Content="Načíst" Command="{Binding LoadCommand}"/>
    <Button Content="Přidat" Command="{Binding AddCommand}"/>
    <DataGrid ItemsSource="{Binding Products}"/>
</Window>
```
Toolkit vygeneruje vlastnosti/commandy za vás, takže kód zůstává krátký a čitelný.

## B) Jak „číst“ migraci
Otevřete soubor `Migrations\2025xxxx_InitialCreate.cs`. Uvidíte např.:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "Categories",
        columns: table => new
        {
            Id = table.Column<int>(nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
            Name = table.Column<string>(maxLength: 100, nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_Categories", x => x.Id);
        });
    // ... další tabulky a relace ...
}
```
- `CreateTable` – vytvoří tabulku se sloupci, typy a omezeními.
- `Annotation("SqlServer:Identity", "1, 1")` – identity sloupec (autoincrement).
- `AddForeignKey`/`CreateIndex` – vazby a indexy.

## C) (Alternativa) Kdyby LocalDB nešla použít
Můžete přepnout na **SQLite**: v NuGet nainstalujte `Microsoft.EntityFrameworkCore.Sqlite` a v `OnConfiguring` použijte
```csharp
optionsBuilder.UseSqlite("Data Source=sklad.db");
```
Ve Visual Studio pak k nahlížení do DB potřebujete rozšíření (např. „SQLite/SQL Server Compact Toolbox“) nebo externí nástroj „DB Browser for SQLite“. Pro tento kurz ale doporučujeme **LocalDB**, protože má ve Visual Studiu **„View Data“** bez doinstalací.

---

## Hotovo!
- Máte **WPF Application (.NET 9)** projekt.
- Přes EF Core **migrace** jste vytvořili DB schéma i seed data.
- Vidíte data v **SQL Server Object Explorer** i v **DataGridu** v okně.
- Vyzkoušeli jste **relace** 1:N, 1:1 a M:N.

> Pokud chcete README aktualizovat (např. přidat další zadání), držte se stejné struktury – studenti budou mít vše na jednom místě.

