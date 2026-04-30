export type { components } from './generated-types';

// ============================================================
// Request Types
// ============================================================

export interface PancakeRequest {
  milk?: string;
  flour?: string;
  eggs?: string;
  toppings: string[];
}

export interface WaffleRequest {
  milk?: string;
  flour?: string;
  eggs?: string;
  butter?: string;
  toppings: string[];
}

export interface OrderRequest {
  customerName?: string;
  items: OrderItemRequest[];
  tableNumber?: number;
}

export interface OrderItemRequest {
  itemType?: string;
  batchId?: string;
  quantity: number;
}

export interface UpdateOrderStatusRequest {
  status?: string;
}

export interface ToppingRequest {
  name?: string;
  category?: string;
}

export interface UpdateToppingRequest {
  name?: string;
  category?: string;
}

export interface DailySpecialOrderRequest {
  specialId?: string;
  quantity: number;
}

export interface InventoryItemRequest {
  name?: string;
  category?: string;
  quantity: number;
  unit?: string;
  reorderLevel: number;
}

export interface ReservationRequest {
  customerName?: string;
  tableNumber: number;
  partySize: number;
  reservedAt: string;
  contactPhone?: string;
}

export interface StaffMemberRequest {
  name?: string;
  role?: string;
  email?: string;
  isActive: boolean;
  hiredAt?: string;
}

// ============================================================
// Response Types
// ============================================================

export interface PancakeResponse {
  batchId: string;
  ingredients: string[];
  toppings: string[];
  createdAt: string;
}

export interface WaffleResponse {
  batchId: string;
  ingredients: string[];
  toppings: string[];
  createdAt: string;
}

export interface OrderResponse {
  orderId: string;
  customerName: string;
  items: OrderItemResponse[];
  tableNumber?: number;
  status: string;
  createdAt: string;
}

export interface OrderItemResponse {
  itemType: string;
  batchId: string;
  quantity: number;
}

export interface ToppingResponse {
  toppingId: string;
  name: string;
  category: string;
}

export interface MilkResponse {
  milk: string;
}

export interface GoatMilkResponse {
  goatMilk: string;
}

export interface EggsResponse {
  eggs: string;
}

export interface FlourResponse {
  flour: string;
}

export interface MenuItemResponse {
  name: string;
  description: string;
  isAvailable: boolean;
  requiredIngredients: string[];
}

export interface DailySpecialResponse {
  specialId: string;
  name: string;
  description: string;
  remainingQuantity: number;
}

export interface DailySpecialOrderResponse {
  orderConfirmationId: string;
  specialId: string;
  quantityOrdered: number;
  remainingQuantity: number;
}

export interface InventoryItemResponse {
  id: number;
  name: string;
  category: string;
  quantity: number;
  unit: string;
  reorderLevel: number;
  lastRestockedAt: string;
  createdAt: string;
}

export interface ReservationResponse {
  id: number;
  customerName: string;
  tableNumber: number;
  partySize: number;
  reservedAt: string;
  status: string;
  contactPhone?: string;
  createdAt: string;
}

export interface StaffMemberResponse {
  id: number;
  name: string;
  role: string;
  email: string;
  isActive: boolean;
  hiredAt: string;
  createdAt: string;
}

export interface AuditLogResponse {
  auditLogId: string;
  action: string;
  entityType: string;
  entityId: string;
  details: string;
  timestamp: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// ============================================================
// Health Check Types
// ============================================================

export interface HealthCheckResponse {
  status: string;
  totalDuration: string;
  entries: Record<string, HealthCheckEntry>;
}

export interface HealthCheckEntry {
  status: string;
  duration: string;
  description?: string;
  tags: string[];
}

// ============================================================
// GraphQL Types
// ============================================================

export interface OrderSummary {
  id: number;
  orderId: string;
  customerName: string;
  itemCount: number;
  tableNumber?: number;
  status: string;
  createdAt: string;
}

export interface RecipeReport {
  id: number;
  orderId: string;
  recipeType: string;
  ingredients: string;
  toppings: string;
  loggedAt: string;
}

export interface IngredientUsage {
  ingredient: string;
  count: number;
}

export interface RecipeTypeCount {
  recipeType: string;
  count: number;
}

export interface Connection<T> {
  edges: Edge<T>[];
  pageInfo: PageInfo;
}

export interface Edge<T> {
  node: T;
  cursor: string;
}

export interface PageInfo {
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  startCursor?: string;
  endCursor?: string;
}

// ============================================================
// Client-Only Union Types
// ============================================================

export type OrderStatus = 'Created' | 'Preparing' | 'Ready' | 'Completed' | 'Cancelled';

export type StaffRole =
  | 'Chef'
  | 'Sous Chef'
  | 'Line Cook'
  | 'Prep Cook'
  | 'Server'
  | 'Host'
  | 'Manager'
  | 'Dishwasher';

export const STAFF_ROLES: StaffRole[] = [
  'Chef', 'Sous Chef', 'Line Cook', 'Prep Cook', 'Server', 'Host', 'Manager', 'Dishwasher',
];

export const ORDER_STATUS_TRANSITIONS: Record<OrderStatus, OrderStatus[]> = {
  Created: ['Preparing', 'Cancelled'],
  Preparing: ['Ready'],
  Ready: ['Completed'],
  Completed: [],
  Cancelled: [],
};

// ============================================================
// Error Types
// ============================================================

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
    public readonly problemDetails?: ProblemDetails,
  ) {
    super(problemDetails?.detail ?? `${status} ${statusText}`);
    this.name = 'ApiError';
  }

  get isValidation(): boolean {
    return this.status === 400;
  }

  get isConflict(): boolean {
    return this.status === 409;
  }

  get isRateLimited(): boolean {
    return this.status === 429;
  }

  get isDownstream(): boolean {
    return this.status === 502;
  }
}
