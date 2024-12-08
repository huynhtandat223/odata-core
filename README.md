# ODataCoreAPI

**ODataCoreAPI** is a lightweight and efficient .NET 9 project designed to simplify API development. By combining the power of OData and Entity Framework Core (EF Core), it allows developers to publish robust APIs with minimal configuration. The project supports seamless CRUD (Create, Read, Update, Delete) operations, enabling quick and efficient development and extension of API solutions.

---

## Features

- **OData Integration**: Offers advanced querying capabilities such as filtering, sorting, pagination, and more.
- **EF Core Integration**: Simplifies database interactions with efficient entity mappings and operations.
- **CRUD Operations**: Provides built-in support for `GET`, `POST`, `PATCH`, and `DELETE` endpoints without extensive setup.
- **Minimal Configuration**: Quickly generate APIs for CRUD operations with OData and EF Core, reducing the need for boilerplate code.
- **Scalable Design**: Built for rapid development while maintaining flexibility for future extensions.

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A SQL-based database for EF Core
- Visual Studio or Visual Studio Code for development

---

## Usage

#### Configuration
```csharp
using CFW.ODataCore;
using Microsoft.EntityFrameworkCore;

//Add your DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

//Add generic ODATA endpoints with asm array is asemplies contains your OData models.
builder.Services
    .AddGenericODataEndpoints([typeof(Program).Assembly]); //default route prefix is odata-api

    ///Others config

var app = builder.Build();
app.UseGenericODataEndpoints();

```

#### Create OData Endpoints

The **category** entity is configured to support OData and CRUD operations as follows:

```csharp

// Configure OData routing for the "categories" endpoint and specify the entity key type as int
[ODataRouting("categories")]
public class Category : IEntity<Guid>, IODataViewModel<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Slug { get; set; }

    public Guid? ParentCategoryId { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}
```

## Explanation of Configuration

Once configured, the `categories` resource supports the following operations:
- **GET**: Query categories or retrieve a single by ID.
- **POST**: Create new categories with details sent in the request body.
- **PATCH**: Update existing categories with partial data.
- **DELETE**: Remove categories by ID.

## Example

### Query categories

**Endpoint**: `GET /odata-api/categories`

**Description**: Fetch a list of categories with OData query options like `$filter`, `$select`, `$orderby`, `$top`, and `$skip`.

---

### Retrieve a Specific category

**Endpoint**: `GET /odata-api/categories/{id}`

**Description**: Retrieve details of a specific category using their unique ID.

---

### Create a New category

**Endpoint**: `POST /odata-api/categories`

**Description**: Create a new category by sending their details in the request body.

---

### Update a category

**Endpoint**: `PATCH /odata-api/categories/{id}`

**Description**: Update a category's details partially by sending only the fields to be modified.

---

### Delete a category

**Endpoint**: `DELETE /odata-api/categories/{id}`

**Description**: Remove a category from the database using their unique ID.
