import express, { type Express } from "express";
import cors from "cors";
import pinoHttp from "pino-http";
import swaggerJsdoc from "swagger-jsdoc";
import swaggerUi from "swagger-ui-express";
import router from "./routes";
import { logger } from "./lib/logger";

const app: Express = express();

app.use(
  pinoHttp({
    logger,
    serializers: {
      req(req) {
        return {
          id: req.id,
          method: req.method,
          url: req.url?.split("?")[0],
        };
      },
      res(res) {
        return {
          statusCode: res.statusCode,
        };
      },
    },
  }),
);
app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

const swaggerSpec = swaggerJsdoc({
  definition: {
    openapi: "3.0.0",
    info: {
      title: "AgriApp - Agricultural Equipment Rental & Maintenance API",
      version: "1.0.0",
      description:
        "Professional API for managing agricultural equipment rentals, maintenance work orders, and customer inquiries. Built with Clean Architecture principles.",
    },
    servers: [{ url: "/api", description: "API Base Path" }],
    components: {
      securitySchemes: {
        bearerAuth: {
          type: "http",
          scheme: "bearer",
          bearerFormat: "JWT",
        },
      },
      schemas: {
        Equipment: {
          type: "object",
          properties: {
            id: { type: "integer" },
            name: { type: "string", example: "John Deere 5405" },
            category: { type: "string", enum: ["Tractor", "Drone", "BioCNG"] },
            hourlyRate: { type: "string", example: "1500.00" },
            centerId: { type: "integer" },
            createdAt: { type: "string", format: "date-time" },
          },
        },
        Inquiry: {
          type: "object",
          properties: {
            id: { type: "integer" },
            customerId: { type: "integer" },
            equipmentId: { type: "integer" },
            salespersonId: { type: "integer" },
            status: { type: "string", enum: ["New", "InProgress", "Converted", "Closed"] },
            centerId: { type: "integer" },
            createdAt: { type: "string", format: "date-time" },
          },
        },
        WorkOrder: {
          type: "object",
          properties: {
            id: { type: "integer" },
            equipmentId: { type: "integer" },
            staffId: { type: "integer" },
            description: { type: "string" },
            status: { type: "string", enum: ["Pending", "InProgress", "Completed", "Cancelled"] },
            centerId: { type: "integer" },
            createdAt: { type: "string", format: "date-time" },
          },
        },
        LoginRequest: {
          type: "object",
          required: ["email", "password"],
          properties: {
            email: { type: "string", example: "admin@agriapp.com" },
            password: { type: "string", example: "SuperUser123!" },
          },
        },
        AuthResponse: {
          type: "object",
          properties: {
            token: { type: "string" },
            user: {
              type: "object",
              properties: {
                id: { type: "integer" },
                name: { type: "string" },
                email: { type: "string" },
                role: { type: "string" },
                centerId: { type: "integer", nullable: true },
              },
            },
          },
        },
        GstQuote: {
          type: "object",
          properties: {
            equipment: { type: "object" },
            hours: { type: "number" },
            pricing: {
              type: "object",
              properties: {
                baseAmount: { type: "number" },
                cgst: { type: "number" },
                sgst: { type: "number" },
                totalGst: { type: "number" },
                grandTotal: { type: "number" },
              },
            },
            commission: {
              type: "object",
              properties: {
                rentalAmount: { type: "number" },
                commissionRate: { type: "number" },
                commissionAmount: { type: "number" },
                netToCompany: { type: "number" },
              },
            },
          },
        },
      },
    },
    security: [{ bearerAuth: [] }],
    paths: {
      "/auth/login": {
        post: {
          tags: ["Authentication"],
          summary: "Login with email and password",
          security: [],
          requestBody: {
            required: true,
            content: { "application/json": { schema: { $ref: "#/components/schemas/LoginRequest" } } },
          },
          responses: {
            "200": {
              description: "Successful login",
              content: { "application/json": { schema: { $ref: "#/components/schemas/AuthResponse" } } },
            },
            "401": { description: "Invalid credentials" },
          },
        },
      },
      "/auth/register": {
        post: {
          tags: ["Authentication"],
          summary: "Register a new user",
          security: [],
          requestBody: {
            required: true,
            content: {
              "application/json": {
                schema: {
                  type: "object",
                  required: ["name", "email", "password"],
                  properties: {
                    name: { type: "string" },
                    email: { type: "string" },
                    password: { type: "string" },
                    role: { type: "string", enum: ["SuperUser", "Manager", "Supervisor", "Sales", "Staff"] },
                    centerId: { type: "integer", nullable: true },
                  },
                },
              },
            },
          },
          responses: { "201": { description: "User registered" }, "409": { description: "Email taken" } },
        },
      },
      "/equipment": {
        get: {
          tags: ["Equipment"],
          summary: "List equipment (filtered by CenterId)",
          responses: { "200": { description: "Equipment list" } },
        },
        post: {
          tags: ["Equipment"],
          summary: "Create equipment (Manager/SuperUser only)",
          requestBody: {
            required: true,
            content: {
              "application/json": {
                schema: {
                  type: "object",
                  required: ["name", "category", "hourlyRate", "centerId"],
                  properties: {
                    name: { type: "string", example: "John Deere 5405" },
                    category: { type: "string", enum: ["Tractor", "Drone", "BioCNG"] },
                    hourlyRate: { type: "number", example: 1500 },
                    centerId: { type: "integer" },
                  },
                },
              },
            },
          },
          responses: { "201": { description: "Equipment created" } },
        },
      },
      "/equipment/{id}": {
        get: {
          tags: ["Equipment"],
          summary: "Get equipment by ID",
          parameters: [{ name: "id", in: "path", required: true, schema: { type: "integer" } }],
          responses: { "200": { description: "Equipment details" }, "404": { description: "Not found" } },
        },
        put: {
          tags: ["Equipment"],
          summary: "Update equipment",
          parameters: [{ name: "id", in: "path", required: true, schema: { type: "integer" } }],
          responses: { "200": { description: "Equipment updated" } },
        },
        delete: {
          tags: ["Equipment"],
          summary: "Delete equipment",
          parameters: [{ name: "id", in: "path", required: true, schema: { type: "integer" } }],
          responses: { "200": { description: "Equipment deleted" } },
        },
      },
      "/equipment/{id}/quote": {
        post: {
          tags: ["Equipment"],
          summary: "Get rental quote with GST and commission breakdown",
          parameters: [{ name: "id", in: "path", required: true, schema: { type: "integer" } }],
          requestBody: {
            required: true,
            content: {
              "application/json": {
                schema: {
                  type: "object",
                  required: ["hours"],
                  properties: { hours: { type: "number", example: 8 } },
                },
              },
            },
          },
          responses: {
            "200": {
              description: "Quote with GST and commission",
              content: { "application/json": { schema: { $ref: "#/components/schemas/GstQuote" } } },
            },
          },
        },
      },
      "/inquiries": {
        get: {
          tags: ["Inquiries"],
          summary: "List inquiries (Sales users see only their own)",
          responses: { "200": { description: "Inquiry list" } },
        },
        post: {
          tags: ["Inquiries"],
          summary: "Create inquiry",
          requestBody: {
            required: true,
            content: {
              "application/json": {
                schema: {
                  type: "object",
                  required: ["customerId", "equipmentId", "salespersonId"],
                  properties: {
                    customerId: { type: "integer" },
                    equipmentId: { type: "integer" },
                    salespersonId: { type: "integer" },
                    centerId: { type: "integer" },
                  },
                },
              },
            },
          },
          responses: { "201": { description: "Inquiry created" } },
        },
      },
      "/inquiries/{id}": {
        get: {
          tags: ["Inquiries"],
          summary: "Get inquiry by ID",
          parameters: [{ name: "id", in: "path", required: true, schema: { type: "integer" } }],
          responses: { "200": { description: "Inquiry details" } },
        },
      },
      "/inquiries/{id}/status": {
        patch: {
          tags: ["Inquiries"],
          summary: "Update inquiry status",
          parameters: [{ name: "id", in: "path", required: true, schema: { type: "integer" } }],
          requestBody: {
            required: true,
            content: {
              "application/json": {
                schema: {
                  type: "object",
                  required: ["status"],
                  properties: { status: { type: "string", enum: ["New", "InProgress", "Converted", "Closed"] } },
                },
              },
            },
          },
          responses: { "200": { description: "Status updated" } },
        },
      },
      "/work-orders": {
        get: {
          tags: ["Work Orders"],
          summary: "List work orders (filtered by CenterId)",
          responses: { "200": { description: "Work order list" } },
        },
        post: {
          tags: ["Work Orders"],
          summary: "Create work order (Supervisor+ only)",
          requestBody: {
            required: true,
            content: {
              "application/json": {
                schema: {
                  type: "object",
                  required: ["equipmentId", "staffId", "description"],
                  properties: {
                    equipmentId: { type: "integer" },
                    staffId: { type: "integer" },
                    description: { type: "string" },
                    centerId: { type: "integer" },
                  },
                },
              },
            },
          },
          responses: { "201": { description: "Work order created" } },
        },
      },
      "/work-orders/{id}": {
        get: {
          tags: ["Work Orders"],
          summary: "Get work order by ID",
          parameters: [{ name: "id", in: "path", required: true, schema: { type: "integer" } }],
          responses: { "200": { description: "Work order details" } },
        },
      },
      "/work-orders/{id}/status": {
        patch: {
          tags: ["Work Orders"],
          summary: "Update work order status",
          parameters: [{ name: "id", in: "path", required: true, schema: { type: "integer" } }],
          requestBody: {
            required: true,
            content: {
              "application/json": {
                schema: {
                  type: "object",
                  required: ["status"],
                  properties: { status: { type: "string", enum: ["Pending", "InProgress", "Completed", "Cancelled"] } },
                },
              },
            },
          },
          responses: { "200": { description: "Status updated" } },
        },
      },
      "/users": {
        get: {
          tags: ["Users"],
          summary: "List all users (Manager/SuperUser only)",
          responses: { "200": { description: "User list" } },
        },
      },
      "/users/me": {
        get: {
          tags: ["Users"],
          summary: "Get current user profile",
          responses: { "200": { description: "Current user" } },
        },
      },
      "/healthz": {
        get: {
          tags: ["Health"],
          summary: "Health check",
          security: [],
          responses: { "200": { description: "Healthy" } },
        },
      },
    },
  },
  apis: [],
});

app.use("/api/docs", swaggerUi.serve, swaggerUi.setup(swaggerSpec));

app.use("/api", router);

export default app;
