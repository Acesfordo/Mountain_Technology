# API Documentation

## Overview

This document provides comprehensive API documentation for the eShop microservices architecture. Each service exposes RESTful APIs for different aspects of the e-commerce platform.

## Services

### 1. Catalog API

**Base URL:** `/catalog-api`

**Description:** Manages product catalog, including items, brands, and types.

#### Endpoints

##### Get Catalog Items
```http
GET /api/v1/catalog/items
```

**Query Parameters:**
- `pageSize` (int, optional): Number of items per page (default: 10)
- `pageIndex` (int, optional): Page number (default: 0)
- `ids` (string, optional): Comma-separated list of item IDs

**Response:**
```json
{
  "pageIndex": 0,
  "pageSize": 10,
  "count": 50,
  "data": [
    {
      "id": 1,
      "name": "Product Name",
      "description": "Product description",
      "price": 19.99,
      "pictureFileName": "product.jpg",
      "catalogTypeId": 1,
      "catalogType": "Type Name",
      "catalogBrandId": 1,
      "catalogBrand": "Brand Name",
      "availableStock": 100
    }
  ]
}
```

##### Get Catalog Item by ID
```http
GET /api/v1/catalog/items/{id}
```

**Path Parameters:**
- `id` (int, required): The catalog item ID

**Response:**
```json
{
  "id": 1,
  "name": "Product Name",
  "description": "Product description",
  "price": 19.99,
  "pictureFileName": "product.jpg",
  "catalogTypeId": 1,
  "catalogType": "Type Name",
  "catalogBrandId": 1,
  "catalogBrand": "Brand Name",
  "availableStock": 100
}
```

##### Get Catalog Types
```http
GET /api/v1/catalog/catalogtypes
```

**Response:**
```json
[
  {
    "id": 1,
    "type": "Mug"
  },
  {
    "id": 2,
    "type": "T-Shirt"
  }
]
```

##### Get Catalog Brands
```http
GET /api/v1/catalog/catalogbrands
```

**Response:**
```json
[
  {
    "id": 1,
    "brand": ".NET"
  },
  {
    "id": 2,
    "brand": "Azure"
  }
]
```

---

### 2. Basket API

**Base URL:** `/basket-api`

**Description:** Manages shopping baskets for users.

#### Endpoints

##### Get Basket
```http
GET /api/v1/basket/{buyerId}
```

**Path Parameters:**
- `buyerId` (string, required): The buyer/user ID

**Response:**
```json
{
  "buyerId": "user123",
  "items": [
    {
      "id": "item1",
      "productId": 1,
      "productName": "Product Name",
      "unitPrice": 19.99,
      "quantity": 2,
      "pictureUrl": "https://example.com/product.jpg"
    }
  ]
}
```

##### Update Basket
```http
POST /api/v1/basket
```

**Request Body:**
```json
{
  "buyerId": "user123",
  "items": [
    {
      "id": "item1",
      "productId": 1,
      "productName": "Product Name",
      "unitPrice": 19.99,
      "quantity": 2,
      "pictureUrl": "https://example.com/product.jpg"
    }
  ]
}
```

**Response:**
```json
{
  "buyerId": "user123",
  "items": [...]
}
```

##### Checkout Basket
```http
POST /api/v1/basket/checkout
```

**Request Body:**
```json
{
  "buyerId": "user123",
  "city": "Seattle",
  "street": "123 Main St",
  "state": "WA",
  "country": "USA",
  "zipCode": "98101",
  "cardNumber": "4111111111111111",
  "cardHolderName": "John Doe",
  "cardExpiration": "12/25",
  "cardSecurityNumber": "123"
}
```

**Response:** `202 Accepted`

---

### 3. Ordering API

**Base URL:** `/ordering-api`

**Description:** Manages customer orders and order processing.

#### Endpoints

##### Get Orders
```http
GET /api/v1/orders
```

**Query Parameters:**
- `pageSize` (int, optional): Number of orders per page
- `pageIndex` (int, optional): Page number

**Headers:**
- `Authorization: Bearer {token}` (required)

**Response:**
```json
{
  "pageIndex": 0,
  "pageSize": 10,
  "count": 25,
  "data": [
    {
      "orderId": 1,
      "orderDate": "2025-10-23T10:00:00Z",
      "status": "Paid",
      "total": 39.98,
      "items": [...]
    }
  ]
}
```

##### Get Order by ID
```http
GET /api/v1/orders/{id}
```

**Path Parameters:**
- `id` (int, required): The order ID

**Headers:**
- `Authorization: Bearer {token}` (required)

**Response:**
```json
{
  "orderId": 1,
  "orderDate": "2025-10-23T10:00:00Z",
  "status": "Paid",
  "description": "Order description",
  "street": "123 Main St",
  "city": "Seattle",
  "state": "WA",
  "country": "USA",
  "zipCode": "98101",
  "total": 39.98,
  "orderItems": [
    {
      "productId": 1,
      "productName": "Product Name",
      "unitPrice": 19.99,
      "units": 2,
      "pictureUrl": "https://example.com/product.jpg"
    }
  ]
}
```

##### Cancel Order
```http
PUT /api/v1/orders/cancel
```

**Request Body:**
```json
{
  "orderId": 1
}
```

**Headers:**
- `Authorization: Bearer {token}` (required)

**Response:** `200 OK`

##### Ship Order
```http
PUT /api/v1/orders/ship
```

**Request Body:**
```json
{
  "orderId": 1
}
```

**Headers:**
- `Authorization: Bearer {token}` (required)

**Response:** `200 OK`

---

### 4. Webhooks API

**Base URL:** `/webhooks-api`

**Description:** Manages webhook subscriptions for event notifications.

#### Endpoints

##### Get Webhooks
```http
GET /api/v1/webhooks
```

**Headers:**
- `Authorization: Bearer {token}` (required)

**Response:**
```json
[
  {
    "id": 1,
    "url": "https://example.com/webhook",
    "type": "OrderStarted",
    "token": "secret-token"
  }
]
```

##### Create Webhook
```http
POST /api/v1/webhooks
```

**Request Body:**
```json
{
  "url": "https://example.com/webhook",
  "type": "OrderStarted",
  "token": "secret-token"
}
```

**Headers:**
- `Authorization: Bearer {token}` (required)

**Response:** `201 Created`

##### Delete Webhook
```http
DELETE /api/v1/webhooks/{id}
```

**Path Parameters:**
- `id` (int, required): The webhook ID

**Headers:**
- `Authorization: Bearer {token}` (required)

**Response:** `204 No Content`

---

### 5. Mobile BFF Shopping API

**Base URL:** `/mobile-bff`

**Description:** Backend for Frontend service for mobile applications.

#### Endpoints

##### Get Home Data
```http
GET /api/v1/home
```

**Headers:**
- `Authorization: Bearer {token}` (optional)

**Response:**
```json
{
  "featuredItems": [...],
  "brands": [...],
  "types": [...]
}
```

---

## Authentication

### JWT Bearer Token

Most API endpoints require authentication using JWT Bearer tokens.

**How to Obtain a Token:**

1. Authenticate through the identity provider (Azure AD, IdentityServer, etc.)
2. Receive a JWT token
3. Include the token in the `Authorization` header:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Token Expiration

- Access tokens expire after 1 hour
- Refresh tokens can be used to obtain new access tokens
- Implement token refresh logic in your client application

---

## Error Responses

All APIs use standard HTTP status codes:

### Success Codes
- `200 OK` - Request successful
- `201 Created` - Resource created successfully
- `202 Accepted` - Request accepted for processing
- `204 No Content` - Request successful, no content to return

### Client Error Codes
- `400 Bad Request` - Invalid request format or parameters
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource conflict
- `422 Unprocessable Entity` - Validation error

### Server Error Codes
- `500 Internal Server Error` - Server error
- `503 Service Unavailable` - Service temporarily unavailable

### Error Response Format

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "ProductId": ["The ProductId field is required."]
  },
  "traceId": "00-1234567890abcdef-1234567890abcdef-00"
}
```

---

## Rate Limiting

API rate limits (to be implemented):

- **Anonymous users:** 100 requests per minute
- **Authenticated users:** 1000 requests per minute
- **Service-to-service:** Unlimited

Rate limit headers:
```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 950
X-RateLimit-Reset: 1635724800
```

---

## Versioning

APIs are versioned using URL path versioning:

```http
/api/v1/catalog/items
/api/v2/catalog/items
```

**Current Versions:**
- Catalog API: v1
- Basket API: v1
- Ordering API: v1
- Webhooks API: v1

**Deprecation Policy:**
- Older versions will be supported for at least 6 months after a new version is released
- Deprecation notices will be included in API responses
- Documentation will clearly mark deprecated endpoints

---

## OpenAPI/Swagger

Interactive API documentation is available through Swagger UI when the services are running:

- **Catalog API:** `http://localhost:5101/swagger`
- **Basket API:** `http://localhost:5103/swagger`
- **Ordering API:** `http://localhost:5102/swagger`
- **Webhooks API:** `http://localhost:5113/swagger`

**OpenAPI Specification:**
- Download OpenAPI specs from `/swagger/v1/swagger.json`

---

## Testing APIs

### Using cURL

```bash
# Get catalog items
curl -X GET "http://localhost:5101/api/v1/catalog/items?pageSize=10" \
  -H "accept: application/json"

# Get basket (authenticated)
curl -X GET "http://localhost:5103/api/v1/basket/user123" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "accept: application/json"
```

### Using HTTP Files (VS Code REST Client)

See `tests/*.http` files for examples (to be created).

---

## Client SDKs

### .NET Client

```csharp
// Example usage (to be implemented)
var client = new CatalogApiClient("http://localhost:5101");
var items = await client.GetCatalogItemsAsync(pageSize: 10);
```

### TypeScript/JavaScript Client

```typescript
// Example usage (to be implemented)
const client = new CatalogApiClient('http://localhost:5101');
const items = await client.getCatalogItems({ pageSize: 10 });
```

---

## Support

For API questions or issues:
- Open an issue in the GitHub repository
- Check the troubleshooting guide
- Review the application logs

---

## Change Log

### v1.0 (Current)
- Initial API documentation
- All services using v1 API

---

**Last Updated:** October 23, 2025  
**API Version:** 1.0
