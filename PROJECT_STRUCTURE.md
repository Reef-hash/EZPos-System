# EZPos System - Project Structure & Architecture Documentation

## 📁 Project Overview

EZPos System is a modern **Point of Sale (POS) application** built with **WPF (.NET 6.0)** using a layered architecture pattern designed to be scalable for SAAS (Software-as-a-Service) multi-user online deployment.

---

## 🏗️ Directory Structure

```
EZPos-System/
├── src/                                  # Main source code
│   ├── UI/                              # Presentation Layer
│   │   ├── Pages/                       # Individual page components
│   │   │   ├── ProductsPage.xaml
│   │   │   ├── ProductsPage.xaml.cs
│   │   │   ├── SalesPage.xaml
│   │   │   ├── SalesPage.xaml.cs
│   │   │   ├── StockPage.xaml
│   │   │   ├── StockPage.xaml.cs
│   │   │   ├── ReportsPage.xaml
│   │   │   ├── ReportsPage.xaml.cs
│   │   │   ├── SalesModeControl.xaml
│   │   │   └── SalesModeControl.xaml.cs
│   │   ├── Resources/                   # UI Resources (coming soon)
│   │   └── Styles/                      # Custom WPF styles (coming soon)
│   │
│   ├── Business/                        # Business Logic Layer
│   │   └── Services/                    # Business services
│   │       └── (To be implemented - Sales, Product, Report services)
│   │
│   ├── DataAccess/                      # Data Access Layer
│   │   └── Repositories/                # Data repositories
│   │       ├── Database.cs              # Database connection & initialization
│   │       ├── ConfigHelper.cs          # Configuration management
│   │       ├── ProductRepository.cs     # Product data operations
│   │       └── SaleRepository.cs        # Sales data operations
│   │
│   ├── Models/                          # Data Models Layer
│   │   └── Domain/                      # Domain entities
│   │       ├── Product.cs               # Product entity
│   │       ├── Sale.cs                  # Sale/Transaction entity
│   │       └── SaleItem.cs              # Sale line item entity
│   │
│   ├── Security/                        # Security & Authentication (Enterprise Ready)
│   │   ├── Authentication/              # User authentication
│   │   │   └── (To be implemented - Login, Token, User verification)
│   │   └── Authorization/               # User roles & permissions
│   │       └── (To be implemented - Role-based access control)
│   │
│   └── Utilities/                       # Utility Functions
│       ├── Helpers/                     # Helper classes
│       │   └── (To be implemented - Formatting, Validation helpers)
│       └── Extensions/                  # Extension methods
│           └── (To be implemented - Custom LINQ extensions, string/number extensions)
│
├── Resources/                           # Application Resources
│   ├── Icons/                          # Font Awesome or custom icons (future use)
│   └── Images/                         # Application images & logos
│
├── Config/                             # Configuration Files
│   └── config.ini                      # Application configuration
│
├── Root Files
│   ├── MainWindow.xaml                 # Main application shell/navigation
│   ├── MainWindow.xaml.cs              # Main window code-behind
│   ├── App.xaml                        # Global resource dictionary (colors, styles, brushes)
│   ├── App.xaml.cs                     # Application class
│   ├── Program.cs                      # Application entry point
│   ├── EZPos.csproj                    # Project file
│   ├── EZPos.sln                       # Solution file
│   └── README.md                       # Project readme
```

---

## 🏛️ Architecture Layers

### 1. **Presentation Layer (UI)**
- **Location**: `src/UI/Pages/`
- **Responsibility**: Handles user interface and user interactions
- **Components**:
  - `MainWindow.xaml` - Main application shell with navigation sidebar
  - `SalesPage.xaml` - Point of Sale interface
  - `ProductsPage.xaml` - Product inventory management
  - `StockPage.xaml` - Stock level monitoring
  - `ReportsPage.xaml` - Analytics & business intelligence

### 2. **Business Logic Layer**
- **Location**: `src/Business/Services/`
- **Responsibility**: Contains business rules and logic (to be implemented)
- **Future Services**:
  - `SalesService` - Sales calculations & transaction processing
  - `ProductService` - Product management logic
  - `ReportService` - Report generation & analytics
  - `InventoryService` - Stock management logic

### 3. **Data Access Layer**
- **Location**: `src/DataAccess/Repositories/`
- **Responsibility**: Database operations and data retrieval
- **Components**:
  - `Database.cs` - Database connection management
  - `ProductRepository.cs` - Product data access
  - `SaleRepository.cs` - Sales/transaction data access
  - `ConfigHelper.cs` - Application configuration

### 4. **Data Models Layer**
- **Location**: `src/Models/Domain/`
- **Responsibility**: Domain entity definitions
- **Entities**:
  - `Product.cs` - Product model (ID, Name, Price, Stock, Barcode)
  - `Sale.cs` - Transaction model (ID, Date, Total, PaymentMethod)
  - `SaleItem.cs` - Line item model (ProductID, Quantity, Price, Total)

### 5. **Security Layer** (Enterprise Ready)
- **Location**: `src/Security/`
- **Responsibility**: Authentication & Authorization (for SAAS multi-user)
- **Planned Features**:
  - User authentication (login/logout)
  - JWT token generation
  - Role-based access control (RBAC)
  - User permissions management

### 6. **Utilities Layer**
- **Location**: `src/Utilities/`
- **Responsibility**: Common utility functions
- **Helpers**: Date/number formatting, validation
- **Extensions**: Custom LINQ queries, string extensions

---

## 🎨 Design System

### Color Scheme - Modern Minimal Dark Mode
- **Primary**: `#00D9FF` (Cyan)
- **Primary Dark**: `#00B8D4` (Teal)
- **Sidebar**: `#0F172A` (Dark Blue)
- **Content**: `#1E293B` (Dark Gray)
- **Cards**: `#334155` (Medium Gray)
- **Text Primary**: `#F1F5F9` (Light Gray)
- **Text Secondary**: `#94A3B8` (Medium Gray)
- **Success**: `#10B981` (Green)
- **Warning**: `#F59E0B` (Orange)
- **Error**: `#FFEF4444` (Red)

### Icons
- **Library**: FontAwesome.Sharp v6.3.0
- **Icons Used**:
  - Sales: `DollarSign`
  - Products: `Box`
  - Stock: `Warehouse`
  - Reports: `ChartLine`
  - Settings: `Cog`
  - Search: `MagnifyingGlass`
  - Edit: `PencilAlt`
  - Delete: `TrashAlt`
  - Refresh: `RotateRight`

---

## 📦 NuGet Dependencies

```xml
<ItemGroup>
    <PackageReference Include="System.Data.SQLite" Version="1.0.117" />
    <PackageReference Include="FontAwesome.Sharp" Version="6.3.0" />
</ItemGroup>
```

- **System.Data.SQLite** - Database access (SQLite)
- **FontAwesome.Sharp** - Professional icon library for WPF

---

## 🔄 Namespace Structure

### Before Organization
```
namespace DataAccess { }
namespace Models { }
namespace EZPos.UI { }
```

### After Organization (Current)
```
namespace EZPos.DataAccess.Repositories { }
namespace EZPos.Models.Domain { }
namespace EZPos.UI { }
namespace EZPos.UI.Pages { }
namespace EZPos.Business.Services { }          (To be implemented)
namespace EZPos.Security.Authentication { }    (To be implemented)
namespace EZPos.Security.Authorization { }     (To be implemented)
namespace EZPos.Utilities.Helpers { }          (To be implemented)
namespace EZPos.Utilities.Extensions { }       (To be implemented)
```

---

## 🚀 Getting Started

### Build the Project
```bash
dotnet build
```

### Run the Application
```bash
dotnet run
```

### Database Initialization
- Database is automatically initialized on first run
- SQLite database file: `EZPos.db`
- Configuration file: `Config/config.ini`

---

## 📋 Features Implemented

### ✅ Completed
- Modern UI/UX with dark theme
- Main dashboard with navigation
- Sales Page (POS interface)
- Products Management Page
- Stock Inventory Page
- Reports & Analytics Dashboard
- Professional icon library integration
- Organized project structure

### ⏳ Planned Features

#### Business Layer (Phase 2)
- Sales service with calculations
- Product service with CRUD operations
- Inventory management service
- Report generation engine

#### Security Layer (Phase 3)
- User authentication system
- JWT token support
- Role-based access control (Admin, Cashier, Manager)
- User permission management

#### Scalability Features (Phase 4)
- Multi-user support
- Cloud database integration
- API endpoints for web clients
- Real-time sync across terminals
- Audit logging

---

## 🔧 Configuration

### config.ini
Located in `Config/config.ini`, contains:
```ini
[Database]
Path=EZPos.db

[Settings]
AppName=EZPos System
Version=1.0.0
```

---

## 📚 Coding Standards

### Naming Conventions
- **Classes**: PascalCase (e.g., `ProductRepository`)
- **Methods**: PascalCase (e.g., `GetByBarcode()`)
- **Properties**: PascalCase (e.g., `ProductName`)
- **Private Fields**: _camelCase (e.g., `_dbConnection`)
- **Constants**: UPPER_CASE (e.g., `DATABASE_VERSION`)

### File Organization
- One class per file (with exceptions for converters/nested classes in XAML code-behind)
- Namespace hierarchy matches folder structure
- XAML and code-behind together in same folder

### Documentation
- XML comments on public methods
- Clear class-level documentation
- Meaningful variable names

---

## 🔐 Security Roadmap (For SAAS)

1. **Authentication**
   - Local user accounts
   - LDAP integration (for enterprise)
   - OAuth/SSO support

2. **Authorization**
   - Role-based access control
   - Resource-level permissions
   - Audit trail logging

3. **Data Protection**
   - Encrypted passwords (bcrypt/Argon2)
   - HTTPS/TLS for communication
   - Data encryption at rest

4. **Multi-tenancy**
   - Separate data per tenant
   - Tenant isolation in database
   - API key management

---

## 📱 Deployment Scenarios

### Desktop (Current)
- Single-machine SQLite deployment
- Standalone executable

### SAAS (Future)
- Cloud-hosted backend (Azure/AWS)
- Web-based dashboard
- Mobile app support
- Multiple terminals sync

---

## 🐛 Troubleshooting

### Common Issues
1. **Database not found**: Check `Config/config.ini` path
2. **Icons not showing**: Ensure FontAwesome.Sharp package is restored
3. **UI elements misaligned**: Clear obj/bin folders and rebuild

### Debug Mode
```bash
dotnet run --configuration Debug
```

---

## 📝 Contributing

### Adding New Features
1. Create in appropriate layer (UI/Business/DataAccess)
2. Follow namespace structure
3. Update this documentation
4. Test thoroughly

### Code Review Checklist
- [ ] Correct namespace structure
- [ ] Following naming conventions
- [ ] Proper error handling
- [ ] Documentation updated
- [ ] No hardcoded values

---

## 📞 Support & Documentation

- **Tech Stack**: WPF, .NET 6.0-windows, SQLite, C#
- **Target OS**: Windows 10+
- **Database**: SQLite3
- **UI Framework**: WPF with Custom Styling

---

**Last Updated**: May 1, 2026
**Version**: 1.0 - Architecture Foundation
**Status**: Active Development
