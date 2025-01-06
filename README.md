# Entity Minimal API
**Entity Minimal API** is a lightweight framework built on **ASP.NET Core OData** and **Minimal API**, fully compatible with **.NET Core 8**. It is designed to simplify entity management by focusing on clean, extensible handlers. Instead of manually managing HTTP status codes and request-response models, it leverages a **Result Pattern** to streamline operation outcomes and take advantage of the latest improvements in **.NET Core 8**.

---

### Installation
Using .NET CLI:
```bash
dotnet add package CFW.EntityMinimalApi
```

Using Package Manager:
```bash
Install-Package CFW.EntityMinimalApi
```

In the ASP.NET Core Mimimal API project, update your `Program.cs` as below:

```C#
var builder = WebApplication.CreateBuilder(args);

//Register your DbContext
builder.Services.AddDbContext<SampleDbContext>(
               options => options
               .ReplaceService<IModelCustomizer, AutoScanModelCustomizer<SampleDbContext>>() //Optional: Auto register entities with marker interface
               .EnableSensitiveDataLogging()
               .UseSqlite($@"Data Source=sample_db.db"));

//Add Entity Minimal API with default route prefix "odata-api"
builder.Services.AddEntityMinimalApi(o => o.UseDefaultDbContext<SampleDbContext>());

....

var app = builder.Build();

// Use Entity Minimal API
app.UseEntityMinimalApi();

```

Create a new ViewModel or DbModel class and add the `[Entity]` attribute to it. The framework will automatically generate CRUD endpoints for the entity.:
```C#
[Entity("products")]
public class Product : IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public Category Category { get; set; }
    public IEnumerable<Order> Orders { get; set; }
}
```
The framework will automatically generate CRUD endpoints for the `Product` entity. You can customize the generated endpoints by implementing custom handlers.

---

## Key Features
1. [Common CRUD Entity API](#1-common-crud-entity-api)
   - [GET `/products` or `/products/{key}` with OData Query Options](#get-products-or-productskey-with-odata-query-options)
   - [POST `/products` with 1-Level Nested Relationships](#post-products-with-1-level-nested-relationships)
   - [PATCH `/products/{key}` with Partial Updates](#patch-productskey-with-partial-updates)
   - [DELETE `/products/{key}`](#delete-productskey)
2. [Entity Operations](#2-entity-operations)
   - [Function `GET` Non Key-Bound](#example-function-get-non-key-bound)
   - [Action `POST` Key-Bound](#example-action-post-key-bound)
3. [Unbound Operations](#3-unbound-operations)
   - [Unbound Function `GET`](#example-unbound-function-get)
   - [Unbound Action `POST`](#example-unbound-action-post)
4. [Authentication and Authorization](#4-authentication-and-authorization)
5. [ViewModel Support](#5-viewmodel-support)
6. [Automatic Entity Configuration](#6-automatic-entity-configuration)

---


### **1. Common CRUD Entity API**
Automatically generate CRUD endpoints (POST, GET, DELETE, PATCH) for database entities or ViewModels with minimal configuration. You can restrict generated endpoints use `Methods` property of `EntityAttribute` attribute.
#### Example:
```csharp
[Entity("products")]
//[Entity("products", Methods = [EntityMethods.Get, EntityMethods.Post])] // Only GET and POST methods are allowed
public class Product : IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public Category Category { get; set; }
    public IEnumerable<Order> Orders { get; set; }
}
```
### Generated endpoints:

| HTTP Method | Endpoint           | Description                                   | Custom Handler (override default implementation)                            |
|-------------|--------------------|-----------------------------------------------|--------------------------------------------|
| **POST**    | `/products`        | Create a new `Product`.                       | `IEntityCreateHandler<TEntity>`            |
| **PATCH**   | `/products/{id}`   | Update specific fields of an existing `Product`. | `IEntityPatchHandler<TEntity, TKey>`            |
| **GET**     | `/products`        | Retrieve a list of all `Products`.            | `IEntityQueryHandler<TEntity>`              |
| **GET**     | `/products/{id}`   | Retrieve details of a specific `Product` by ID. | `IEntityGetByKeyHandler<TEntity, TKey>`               |
| **DELETE**  | `/products/{id}`   | Delete an existing `Product` by ID.           | `IEntityDeleteHandler<TEntity, TKey>`            |

---

#### GET: Query and get by key with OData query options:

The following OData query options are supported for both:
- **GET `/products`**: Retrieve a list of all products with query capabilities.
- **GET `/products/{id}`**: Retrieve a specific product by ID with additional query capabilities.

| Query Option   | Example                               | Description                                   | SQL Output Example                          |
|----------------|---------------------------------------|-----------------------------------------------|---------------------------------------------|
| **$filter**    | `/products?$filter=price gt 50`       | Filter products with a price greater than $50. | `SELECT Id, Name, Price, CategoryId FROM Products WHERE Price > 50;` |
| **$select**    | `/products?$select=name,price`        | Select only the `name` and `price` fields.     | `SELECT Name, Price FROM Products;`         |
| **$orderby**   | `/products?$orderby=price desc`       | Order products by price in descending order.   | `SELECT Id, Name, Price, CategoryId FROM Products ORDER BY Price DESC;` |
| **$top**       | `/products?$top=5`                   | Retrieve the top 5 products.                  | `SELECT TOP 5 Id, Name, Price, CategoryId FROM Products;`             |
| **$skip**      | `/products?$skip=10`                 | Skip the first 10 products.                   | `SELECT Id, Name, Price, CategoryId FROM Products OFFSET 10 ROWS;`    |
| **$expand**    | `/products?$expand=category`         | Expand related `category` data for each product. | `SELECT p.Id, p.Name, p.Price, c.Id AS CategoryId, c.Name AS CategoryName FROM Products p JOIN Categories c ON p.CategoryId = c.Id;` |

---
#### POST `/products` with 1-Level nested relationships:

The framework supports creating:
1. **Main Entity**: Insert a single entity into the database.
2. **Child Entities (1 Level Nested)**: Handle child entities (either a single complex entity or a collection of entities) nested directly under the main entity.  
   - **Existing Child Entities**: If the child item's ID exists in the database, it is ignored.
   - **New Child Entities**: If the child item's ID is null or not in the database, it is inserted.

| Scenario                          | Request Body                                                                                                                                         | Explanation                                                                                                                                                          |
|-----------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Main Entity Only**              | ```{ "name": "Product A", "price": 150.0 } ```                                                                                                  | Inserts the `Product` entity without any nested relationships.                                                                                                       |
| **Main Entity with Single Child** | ```{ "name": "Product B", "price": 120.0, "category": { "name": "Electronics" } } ```                                                            | Inserts the `Product` entity and the `Category` entity if it doesn't exist. If the `Category` exists, it is associated with the `Product`.                          |
| **Main Entity with Child List**   | ```{ "name": "Product C", "price": 200.0, "orders": [ { "id": 1, "quantity": 2 }, { "quantity": 3 } ] } ```                                      | Inserts the `Product` entity. Existing `Orders` (with `id`) are associated, and new `Orders` (without `id`) are inserted and associated.                            |

---

#### PATCH `/products/{id}` with partial updates:

The PATCH method supports updating specific fields of an existing <strong>ROOT</strong> entity. The request body should contain only the fields that need to be updated.

| Scenario              | Request Body                                                                                         | SQL Generated                                                                                   | Explanation                                                                                  |
|-----------------------|-----------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------|
| **Update Name Only**  | ```json { "name": "Updated Product A" } ```                                                         | ```sql UPDATE Products SET Name = 'Updated Product A' WHERE Id = 1; ```                        | Updates the `name` of the `Product` entity while leaving other fields unchanged.            |
| **Update Price Only** | ```json { "price": 175.0 } ```                                                                      | ```sql UPDATE Products SET Price = 175.0 WHERE Id = 2; ```                                     | Updates the `price` of the `Product` entity while leaving other fields unchanged.           |
| **Update Multiple**   | ```json { "name": "Updated Product B", "price": 200.0 } ```                                         | ```sql UPDATE Products SET Name = 'Updated Product B', Price = 200.0 WHERE Id = 3; ```        | Updates both `name` and `price` fields of the `Product` entity while leaving others intact. |

---
#### DELETE `/products/{id}`:

| Scenario              | SQL Generated                                                   | Explanation                                                                                  |
|-----------------------|-----------------------------------------------------------------|----------------------------------------------------------------------------------------------|
| **Delete by ID**      | ```sql DELETE FROM Products WHERE Id = 1; ```                   | Deletes the `Product` entity with ID `1`.                                                   |

---

### **2. Entity operations**
- **Function**: Defines custom read-only operations bound to an entity or entity set. Functions **do not modify the state** of the data and are invoked via `GET` requests.
- **Action**: Defines custom operations bound to an entity or entity set. Actions **can modify the state** of the data and are invoked via `POST` requests.

#### Function `GET` non key-bound entity set:
```csharp

public class GetDiscountedProducts
{
    public class Request
    {
        public decimal MaxPrice { get; set; }
        public int MaxDiscount { get; set; }
    }
    public class Response
    {
        public IEnumerable<Product> Products { get; set; }
    }

    //URL: GET /products/getDiscountedProducts?maxPrice=100&maxDiscount=20
    [EntityFunction("getDiscountedProducts")]
    public class Handler : IEntityOperationHandler<Product, Request, Response>
    {
        public Task<Result<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            /// Your logic here
        }
    }
}
```
---
#### Function `GET` key-bound entity:
```csharp
public class GetProductDetails
{
    public class Request
    {
        [FromRoute] //or [Key]
        public int ProductId { get; set; }
        public bool IncludeOrders { get; set; }
    }

    public class Response
    {
        public IEnumerable<Product> Products { get; set; }
    }

    // URL: GET /products/{id}/getProductDetails?includeOrders=true
    [EntityFunction("getProductDetails")]
    public class Handler : IEntityOperationHandler<Product, Request, Response>
    {
        public Task<Result<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            /// Your logic here
        }
    }
}
```
---
#### Action `POST` non key-bound entity set:
```csharp
public class AddProduct
{
    public class Request
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class Response
    {
        public Product Product { get; set; }
    }

    //URL: POST /products/addProduct with request json body: { "name": "Product A", "price": 150.0 }
    [EntityAction("addProduct")]
    public class Handler : IEntityOperationHandler<Product, Request, Response>
    {
        public Task<Result<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            /// Your logic here
        }
    }
}
```
---
#### Action `POST` key-bound entity:
```csharp
public class AddOrder
{
    public class Request
    {
        [FromRoute] //or [Key]
        public int ProductId { get; set; }

        public string OrderNumber { get; set; }
    }

    public class Response
    {
        public Order Order { get; set; }
    }

    //URL: POST /products/{id}/addOrder with request json body: { "orderNumber": "Order 1" }
    [EntityAction("addOrder")]
    public class Handler : IEntityOperationHandler<Product, Request, Response>
    {
        public Task<Result<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            /// Your logic here
        }
    }
}
```
---
#### Action `POST` non key-bound entity with no response
```csharp
public class DeleteAllOrders
{
    public class Request
    {
        public int ProductId { get; set; }
    }

    //URL: POST /products/deleteAllOrders with request json body: { "productId": 1 }
    [EntityAction("deleteAllOrders")]
    public class Handler : IEntityOperationHandler<Product, Request>
    {
        public Task<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            /// Your logic here
        }
    }
}
```
---
#### Action `POST` key-bound entity with no response
```csharp
public class DeleteOrder
{
    public class Request
    {
        [FromRoute] //or [Key]
        public int ProductId { get; set; }
        public int OrderId { get; set; }
    }

    //URL: POST /products/{id}/deleteOrder with request json body: { "orderId": 1 }
    [EntityAction("deleteOrder")]
    public class Handler : IEntityOperationHandler<Product, Request>
    {
        public Task<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            /// Your logic here
        }
    }
}
```
---
### **3. Unbound operations**
- **Unbound function**: Defines custom read-only operations that are not bound to any entity or entity set. Functions **do not modify the state** of the data and are invoked via `GET` requests.
- **Unbound action**: Defines custom operations that are not bound to any entity or entity set. Actions **can modify the state** of the data and are invoked via `POST` requests.

#### Unbound Function `GET` with non key-bound entity set:
```csharp
public class GetProductsWithDiscount
{
    public class Request
    {
        public decimal MaxPrice { get; set; }
        public int MaxDiscount { get; set; }
    }

    public class Response
    {
        public IEnumerable<Product> Products { get; set; }
    }

    //URL: GET /getProductsWithDiscount?maxPrice=100&maxDiscount=20
    [UnboundFunction("getProductsWithDiscount")]
    public class Handler : IUnboundOperationHandler<Request, Response>
    {
        public Task<Result<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            /// Your logic here
        }
    }
}
```
---
#### Unbound Function `GET` with key-bound entity:
```csharp
public class GetProductDetails
{
    public class Request
    {
        [FromRoute] //or [Key]
        public int ProductId { get; set; }
        public bool IncludeOrders { get; set; }
    }

    public class Response
    {
        public IEnumerable<Product> Products { get; set; }
    }

    // URL: GET /getProductDetails/{id}?includeOrders=true
    [UnboundFunction("getProductDetails")]
    public class Handler : IUnboundOperationHandler<Request, Response>
    {
        public Task<Result<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            /// Your logic here
        }
    }
}
```
---
#### Unbound Action `POST` with non key-bound entity set:
```csharp
public class AddProduct
{
    public class Request
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class Response
    {
        public Product Product { get; set; }
    }

    //URL: POST /addProduct with request json body: { "name": "Product A", "price": 150.0 }
    [UnboundAction("addProduct")]
    public class Handler : IUnboundOperationHandler<Request, Response>
    {
        public Task<Result<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            /// Your logic here
        }
    }
}
```
---
#### Unbound Action `POST` with key-bound entity:
```csharp
public class AddOrder
{
    public class Request
    {
        [FromRoute] //or [Key]
        public int ProductId { get; set; }

        public string OrderNumber { get; set; }
    }

    public class Response
    {
        public Order Order { get; set; }
    }

    //URL: POST /addOrder/{id} with request json body: { "orderNumber": "Order 1" }
    [UnboundAction("addOrder")]
    public class Handler : IUnboundOperationHandler<Request, Response>
    {
        public Task<Result<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            /// Your logic here
        }
    }
}
```
---
#### Unbound Action `POST` with no response
```csharp
public class DeleteAllOrders
{
    public class Request
    {
        public int ProductId { get; set; }
    }

    //URL: POST /deleteAllOrders with request json body: { "productId": 1 }
    [UnboundAction("deleteAllOrders")]
    public class Handler : IUnboundOperationHandler<Request>
    {
        public Task<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            /// Your logic here
        }
    }
}
```
---
#### Unbound Action `POST` with no response and key-bound entity:
```csharp
public class DeleteOrder
{
    public class Request
    {
        [FromRoute] //or [Key]
        public int ProductId { get; set; }
        public int OrderId { get; set; }
    }

    //URL: POST /deleteOrder/{id} with request json body: { "orderId": 1 }
    [UnboundAction("deleteOrder")]
    public class Handler : IUnboundOperationHandler<Request>
    {
        public Task<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            /// Your logic here
        }
    }
}
```
---
### **4. Authentication and Authorization**

The framework integrates seamlessly with ASP.NET Core's **authentication** and **authorization** mechanisms to provide secure access to entities and operations. This includes:

1. **Authentication**: Ensures that only authenticated users can access the application.
2. **Authorization**: Enforces role-based and method-specific access control, leveraging:
   - **`EntityAuthorize`**: Custom attribute for CRUD operations, inheriting from ASP.NET Core's `AuthorizeAttribute`.
   - **ASP.NET Core's `AuthorizeAttribute`**: Used for custom entity operations.

By following the ASP.NET Core model, the framework ensures compatibility with existing security configurations, such as `JwtBearer`, `CookieAuthentication`, or external providers (e.g., OAuth, OpenID Connect).

### Example: Configuring Authorization for an Entity
```csharp
[Entity("multi-authorizations")]
[EntityAuthorize(ApplyMethods = new[] { EntityMethod.Query })] // Only authorized users can query the entity.
[EntityAuthorize(ApplyMethods = new[] { EntityMethod.GetByKey }, Roles = TestUtils.AdminRole)] // Admin-only access for GetByKey.
[EntityAuthorize(ApplyMethods = new[] { EntityMethod.Post }, Roles = $"{TestUtils.AdminRole},{TestUtils.SupperAdminRole}")] // Admin and SuperAdmin access for Post.
[EntityAuthorize(ApplyMethods = new[] { EntityMethod.Delete }, Roles = TestUtils.SupperAdminRole)] // SuperAdmin-only access for Delete.
public class MultiAuthorization : IEntity<Guid>
{
    public Guid Id { get; set; }
}
```
---
### Example: Configuring Authorization for an Entity Operation
```csharp
public class DeleteAllOrders
{
    public class Request
    {
        public int ProductId { get; set; }
    }

    [Authorzie(Roles = TestUtils.AdminRole)] // Only Admin can access this action.
    [UnboundAction("deleteAllOrders")]
    public class Handler : IUnboundOperationHandler<Request>
    {
        public Task<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            /// Your logic here
        }
    }
}
```
---
### **5. View model support**
The framework provides basic support for mapping between ViewModels and DbModels. The generated SQL query behavior remains the same, but the response is mapped to the ViewModel.
1. **Automatic Mapping**:
   - If the `ViewModel` and `DbModel` have the same type and property names, the mapping is handled automatically.
2. **Custom Mapping**:
   - If property names differ, the `EntityPropertyNameAttribute` can be used to specify the mapping explicitly.

**DbModel**:
```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

```

**ViewModel**:
```csharp

[Entity("products", DbSetType = typeof(Product))]
public class ProductViewModel
{
    public int Id { get; set; }
    [EntityPropertyName("Name")]
    public string ProductName { get; set; }
    public decimal Price { get; set; }
}
```
---
### **6. Automatic Entity Configuration**

The framework simplifies entity configuration by automatically adding database entities to the `DbContext` using a **marker interface**. This eliminates the need to manually register entities in the `DbContext` class.

#### Example:
```csharp

//Change the marker interface to your own interface if needed, if you use built-in IEntity<> interface, you don't need to set this property.
AutoScanModelCustomizer<SampleDbContext>.EntityMarkerTypes = new[]
{
    typeof(IEntity<>)
};

builder.Services.AddDbContext<SampleDbContext>(
               options => options
               .ReplaceService<IModelCustomizer, AutoScanModelCustomizer<SampleDbContext>>()
               .EnableSensitiveDataLogging()
               .UseSqlite($@"Data Source=sample_db.db"));
```
---