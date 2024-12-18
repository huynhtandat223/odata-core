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

## Installation
### Init your project
1. Clone the repository and create your own project, then add reference to CFW.ODataCore.csproj and CFW.Core.csproj in your project.
    ```bash
    # Clone the repository
    git clone https://github.com/huynhtandat223/odata-core.git

    # Navigate to the project directory
    cd odata-core

    # Create a solution file
    dotnet new sln -n SampleProject
    # Create a new Web API project
    dotnet new webapi -n SampleProject

    # Add the project to the solution
    dotnet sln SampleProject.sln add SampleProject/SampleProject.csproj

    # Add CFW.ODataCore and CFW.Core to the solution
    dotnet sln SampleProject.sln add modules/CFW.ODataCore/CFW.ODataCore.csproj
    dotnet sln SampleProject.sln add modules/CFW.Core/CFW.Core.csproj

    # Add reference to CFW.ODataCore and CFW.Core in your project
    dotnet add SampleProject/SampleProject.csproj reference modules/CFW.ODataCore/CFW.ODataCore.csproj
    dotnet add SampleProject/SampleProject.csproj reference modules/CFW.Core/CFW.Core.csproj

    # Navigate to the project directory
    cd SampleProject

    # Add Ef core provider packages, for example app, I use sqlite
    dotnet add package Microsoft.EntityFrameworkCore.Sqlite
    ```

2. Use visual studio or vs code to open your solution, open Program.cs file and add some initial configuration.
    ```csharp

    using CFW.ODataCore;
    using Microsoft.EntityFrameworkCore;

    //Add your DbContext
    builder.Services.AddDbContext<AppDbContext>(
            options => options.UseSqlite("Data Source=yourdb.db"));

    //Add generic ODATA endpoints with asm array is asemplies contains your OData models. Default route prefix is 'odata-api' and assembly is current assembly.
    builder.Services
        .AddGenericODataEndpoints();

    var app = builder.Build();

    //Use generic OData endpoints
    app.UseGenericODataEndpoints();

    //Create database if not exists, if you use other database provider, you can change it here.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    app.Run();
    ```

3. Create your OData and Entity Framework Core models:

        * ODataRouting attribute is used to define the endpoint name.
        * IEntity<Guid> is used to define the entity and key type, EF core migration will use this interface to create the table.
        * IODataViewModel<Guid> is used to define the entity and key type for OData, it's used to generate OData metadata.

    ```csharp
    using CFW.Core.Entities;
    using CFW.ODataCore.Core;

    namespace SampleProject.Models;

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

    [ODataRouting("products")]
    public class Product : IEntity<Guid>, IODataViewModel<Guid>
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public Category? Category { get; set; } = default!;
    }

    [ODataRouting("orders")]
    public class Order : IEntity<Guid>, IODataViewModel<Guid>
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Note { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
    ```

The OData core API will automatically generate the following endpoints for the models:

    
    * GET /odata-api/categories
    * GET /odata-api/categories/{id}
    * POST /odata-api/categories
    * PATCH /odata-api/categories/{id}
    * DELETE /odata-api/categories/{id}

    * GET /odata-api/products
    * GET /odata-api/products/{id}
    * POST /odata-api/products
    * PATCH /odata-api/products/{id}
    * DELETE /odata-api/products/{id}

    * GET /odata-api/orders
    * GET /odata-api/orders/{id}
    * POST /odata-api/orders
    * PATCH /odata-api/orders/{id}
    * DELETE /odata-api/orders/{id}

All of the endpoints support OData query parameters such as $filter, $orderby, $top, $skip, $select, $expand, etc. You can reference the <a href="https://learn.microsoft.com/en-us/aspnet/web-api/overview/odata-support-in-aspnet-web-api/supporting-odata-query-options">OData documentation</a>  for more information.
Start your project and access to url `/odata-api/$metadata` to see the metadata of your OData API.

---

# Usage 

### Create new OData model

#### Create with no relationship
```json
POST /odata-api/categories
{
    "name": "Category 1",
    "description": "Description of category 1",
    "slug": "category-1",
    "isActive": true,
    "displayOrder": 1
    ....
}
```

#### Create with relationship
```json
POST /odata-api/products 
{
    "name": "Product 1",
    "description": "Description of product 1",
    "category": {
        "id": "category-id"
    }
    ....
}
```

If `category-id` is not supplied or exists, `product` and `category` will be created automatically. If `category-id` is supplied and exists, `product` will be created and associated with the existing `category`.

#### Create with collection relationship
```json
POST /odata-api/orders
{
    "customerId": "customer-id",
    "orderNumber": "order-number",
    "orderDate": "2022-01-01",
    "totalAmount": 100,
    "note": "Note of order",
    "status": "Pending",
    "products": [
        {
            "id": "product-id-1"
        },
        {
            "id": "product-id-2"
        }
    ]
}
```

Collection creation behaviour is similar to single relationship creation.

### Update OData model
```json
PATCH /odata-api/categories/{id}
{
    "name": "Category 1 updated",
    "description": "Description of category 1 updated",
}
```

OData use delta update, only the fields that are supplied will be updated. As example above, only `name` and `description` will be updated, other fields will remain the same. Currently, ODataCoreAPI does not support partial update for relationship and collection relationship.

### Delete OData model
```json
DELETE /odata-api/categories/{id}
```

### Query OData model
```json
GET /odata-api/categories?$filter=name eq 'Category 1'&$select=name,description
```

The SQL generated:
```sql
SELECT [t].[Name], [t].[Description] FROM [Categories] AS [t] WHERE [t].[Name] = N'Category 1'
```

This is powerful feature of OData, you can query, filter, sort, paginate, expand, select, etc. with OData query parameters and it will be translated to SQL query like you use Ef core as backend.

---

I use integration test to test the API, you can see the test in `CFW.ODataCore.Tests` project.

---

Roadmap
- Use React to create a CRUD page 
- Add more features to ODataCoreAPI such as authentication, authorization, etc.
- Flexible API endpoints generation use configuration.