# EZPos Architecture & SAAS Scalability Guide

## 🏗️ Layered Architecture Pattern

EZPos System is built following a **4-Tier Layered Architecture** with future expansion for SAAS multi-user deployment.

```
┌─────────────────────────────────────────────┐
│   PRESENTATION LAYER                        │
│   (UI - MainWindow, Pages, Controls)        │
├─────────────────────────────────────────────┤
│   BUSINESS LOGIC LAYER                      │
│   (Services - Sales, Product, Report)       │
├─────────────────────────────────────────────┤
│   DATA ACCESS LAYER                         │
│   (Repositories - Database operations)      │
├─────────────────────────────────────────────┤
│   DATABASE LAYER                            │
│   (SQLite - Local or Cloud DB)              │
└─────────────────────────────────────────────┘
        ↑                             ↑
        │                             │
   ┌────────────────────┬─────────────────┐
   │  SECURITY LAYER    │  UTILITIES      │
   │  (Auth, Authz)     │  (Helpers, Ext) │
   └────────────────────┴─────────────────┘
```

---

## 📊 Tier Details

### 1️⃣ Presentation Layer (UI)
**Location**: `src/UI/`

**Components**:
- `Pages/` - Individual page components with code-behind
- `Resources/` - XAML resources (converters, brushes)
- `Styles/` - Global style definitions
- `MainWindow.xaml` - Application shell

**Responsibilities**:
- Display data to user
- Capture user input
- Handle UI events
- Navigate between pages

**Example**:
```csharp
namespace EZPos.UI.Pages
{
    public partial class SalesPage : UserControl
    {
        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            // Call business service
            var service = new SalesService();
            service.AddItemToCart(product, quantity);
        }
    }
}
```

---

### 2️⃣ Business Logic Layer
**Location**: `src/Business/Services/` (To be implemented)

**Planned Services**:

#### SalesService
```csharp
namespace EZPos.Business.Services
{
    public class SalesService
    {
        private SaleRepository _saleRepo;
        
        public void ProcessSale(Sale sale)
        {
            // Validate sale
            // Calculate totals
            // Apply discounts
            // Save to database
            _saleRepo.Save(sale);
        }
    }
}
```

#### ProductService
```csharp
public class ProductService
{
    private ProductRepository _productRepo;
    
    public List<Product> GetLowStockProducts()
    {
        return _productRepo.GetAll()
            .Where(p => p.Stock < p.ReorderLevel)
            .ToList();
    }
}
```

#### ReportService
```csharp
public class ReportService
{
    public SalesReport GenerateDailySalesReport(DateTime date)
    {
        // Calculate KPIs
        // Format data for display
        // Return report object
    }
}
```

**Responsibilities**:
- Business rules & validation
- Calculations & transformations
- Orchestrate data operations
- Handle exceptions gracefully

---

### 3️⃣ Data Access Layer
**Location**: `src/DataAccess/Repositories/`

**Current Implementation**:
- `Database.cs` - Connection management
- `ProductRepository.cs` - Product CRUD
- `SaleRepository.cs` - Sales CRUD
- `ConfigHelper.cs` - Config management

**Pattern**: Repository Pattern
```csharp
namespace EZPos.DataAccess.Repositories
{
    public static class ProductRepository
    {
        public static Product GetByBarcode(string barcode)
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();
                // Execute query
                return product;
            }
        }
        
        public static List<Product> GetAll()
        {
            // Return all products
        }
        
        public static void Save(Product product)
        {
            // Insert or update
        }
    }
}
```

**Responsibilities**:
- Database connection management
- SQL execution
- Data mapping (Database ↔ Objects)
- Error handling at DB level

---

### 4️⃣ Domain Models Layer
**Location**: `src/Models/Domain/`

**Entity Classes**:

```csharp
namespace EZPos.Models.Domain
{
    public class Product
    {
        public int Id { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int ReorderLevel { get; set; }
    }

    public class Sale
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public List<SaleItem> Items { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
    }

    public class SaleItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }
}
```

**Responsibilities**:
- Define entity structure
- Represent business concepts
- No business logic (just properties)

---

## 🔐 Security Layer (Enterprise Ready)

**Location**: `src/Security/`

### Authentication Layer
**Path**: `src/Security/Authentication/`

```csharp
namespace EZPos.Security.Authentication
{
    public class AuthenticationService
    {
        public bool Login(string username, string password)
        {
            // Validate credentials
            // Generate JWT token
            // Store in secure location
            return true;
        }
        
        public User GetCurrentUser()
        {
            // Return logged-in user from token
        }
        
        public void Logout()
        {
            // Clear authentication
        }
    }
}
```

### Authorization Layer
**Path**: `src/Security/Authorization/`

```csharp
namespace EZPos.Security.Authorization
{
    public class AuthorizationService
    {
        public bool HasPermission(User user, string permission)
        {
            // Check user role
            // Verify permission
            return user.Roles.Any(r => r.Permissions.Contains(permission));
        }
        
        public bool CanAccessPage(User user, string pageName)
        {
            // Role-based page access
        }
    }
}
```

---

## 🛠️ Utilities Layer

**Location**: `src/Utilities/`

### Helpers
**Path**: `src/Utilities/Helpers/`

```csharp
namespace EZPos.Utilities.Helpers
{
    public static class ValidationHelper
    {
        public static bool IsValidBarcode(string barcode)
        {
            return barcode.Length == 12 && barcode.All(char.IsDigit);
        }
        
        public static bool IsValidPrice(decimal price)
        {
            return price >= 0 && price <= 999999.99m;
        }
    }
    
    public static class FormattingHelper
    {
        public static string FormatCurrency(decimal amount)
        {
            return amount.ToString("C2");
        }
        
        public static string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm");
        }
    }
}
```

### Extensions
**Path**: `src/Utilities/Extensions/`

```csharp
namespace EZPos.Utilities.Extensions
{
    public static class StringExtensions
    {
        public static bool IsValidEmail(this string email)
        {
            // Email validation
            return true;
        }
    }
    
    public static class DateTimeExtensions
    {
        public static string ToFriendlyString(this DateTime date)
        {
            return $"{date:MMM dd, yyyy}";
        }
    }
    
    public static class ListExtensions
    {
        public static decimal Sum(this List<SaleItem> items)
        {
            return items.Sum(i => i.Total);
        }
    }
}
```

---

## 🔄 Data Flow Example

### Sales Transaction Flow

```
User Interface
    ↓
UI Layer: SalesPage.xaml.cs
    User clicks "Checkout"
    ↓
Business Logic: SalesService.ProcessSale()
    - Validate cart items
    - Calculate tax
    - Apply discounts
    - Create Sale object
    ↓
Data Access: SaleRepository.Save()
    - Insert into Sales table
    - Insert into SaleItems table
    - Update Product Stock
    ↓
Database: SQLite
    - Commit transaction
    ↓
Response
    - Success notification
    - Print receipt
    - Reset UI
```

---

## 🌐 SAAS Scalability Roadmap

### Phase 1: Desktop (Current)
- Single machine, local SQLite
- Single user
- Local authentication

### Phase 2: Multi-Terminal Desktop
- Network shared database
- Multiple cashiers
- Centralized reports

### Phase 3: SAAS Cloud
- Cloud backend (Azure/AWS)
- Web API layer
- Multi-tenant support
- User management

### Phase 4: Full SAAS
- Mobile app support
- Real-time inventory sync
- Advanced analytics
- Payment gateway integration

---

## 🔧 Implementing New Features

### Example: Adding a Discount Service

**Step 1**: Create Business Service
```csharp
// src/Business/Services/DiscountService.cs
namespace EZPos.Business.Services
{
    public class DiscountService
    {
        public decimal ApplyDiscount(decimal total, int discountPercent)
        {
            return total * (1 - discountPercent / 100m);
        }
    }
}
```

**Step 2**: Update Repository (if needed)
```csharp
// Add to ProductRepository if storing discounts
```

**Step 3**: Use in UI
```csharp
// In SalesPage.xaml.cs
private void ApplyDiscount_Click(object sender, RoutedEventArgs e)
{
    var discountService = new DiscountService();
    decimal finalTotal = discountService.ApplyDiscount(cartTotal, 10);
}
```

---

## 📱 Future: API Layer (For Web/Mobile)

### Planned API Structure
```
/api/v1/
├── /auth/
│   ├── POST /login
│   ├── POST /logout
│   └── GET /me
├── /products/
│   ├── GET /all
│   ├── GET /{id}
│   └── POST /search
├── /sales/
│   ├── GET /all
│   ├── GET /{id}
│   ├── POST /create
│   └── GET /reports
└── /inventory/
    ├── GET /stock
    ├── PUT /{id}/adjust
    └── GET /low-stock
```

---

## ⚡ Performance Optimization

### Current Optimizations
- Repository pattern for efficient data access
- Use of `using` statements for resource management
- Compiled queries where possible

### Future Optimizations
- Database indexing on commonly searched columns
- Caching layer (Redis)
- Query optimization & pagination
- API rate limiting

---

## 🧪 Testing Strategy

### Unit Tests (Future)
```csharp
[TestClass]
public class SalesServiceTests
{
    [TestMethod]
    public void ProcessSale_CalculatesTotalCorrectly()
    {
        // Arrange
        var service = new SalesService();
        var sale = new Sale { /* ... */ };
        
        // Act
        service.ProcessSale(sale);
        
        // Assert
        Assert.AreEqual(expectedTotal, sale.Total);
    }
}
```

### Integration Tests
- Database operations
- End-to-end workflows
- External integrations

---

## 📚 Dependency Injection (Future)

### Planned Implementation
```csharp
// Startup configuration
services.AddScoped<SalesService>();
services.AddScoped<ProductService>();
services.AddScoped<SaleRepository>();
services.AddScoped<ProductRepository>();
```

---

## 🔗 References

- [Repository Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Clean Code Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

**Last Updated**: May 1, 2026
**Version**: 1.0 - Architecture Guide
