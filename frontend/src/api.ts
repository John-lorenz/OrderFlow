export type UserRole = "Admin" | "Manager" | "Operator";
export type OrderStatus = "Draft" | "Confirmed" | "Paid" | "Shipped" | "Completed" | "Cancelled";

export type AuthResponse = {
  userId: string;
  fullName: string;
  email: string;
  role: UserRole;
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
};

export type Paged<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

export type Product = {
  id: string;
  sku: string;
  name: string;
  description?: string;
  unitPrice: number;
  stockQuantity: number;
  isActive: boolean;
};

export type Customer = {
  id: string;
  name: string;
  email: string;
  document: string;
  phone?: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  isActive: boolean;
};

export type OrderItem = {
  id: string;
  productId: string;
  sku: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
};

export type Order = {
  id: string;
  orderNumber: string;
  customerId: string;
  status: OrderStatus;
  totalAmount: number;
  notes?: string;
  paymentReference?: string;
  trackingNumber?: string;
  cancellationReason?: string;
  createdAtUtc: string;
  items: OrderItem[];
  history: {
    fromStatus: OrderStatus;
    toStatus: OrderStatus;
    reason?: string;
    changedAtUtc: string;
  }[];
};

export type Dashboard = {
  totalOrders: number;
  totalRevenue: number;
  ordersByStatus: Record<string, number>;
  recentOrders: Order[];
};

const TOKEN_KEY = "orderflow.auth";

export function getAuth(): AuthResponse | null {
  const raw = localStorage.getItem(TOKEN_KEY);
  return raw ? (JSON.parse(raw) as AuthResponse) : null;
}

export function setAuth(auth: AuthResponse | null) {
  if (!auth) {
    localStorage.removeItem(TOKEN_KEY);
    return;
  }
  localStorage.setItem(TOKEN_KEY, JSON.stringify(auth));
}

function isExpiringSoon(auth: AuthResponse) {
  const expires = Date.parse(auth.accessTokenExpiresAtUtc);
  return Number.isNaN(expires) || expires - Date.now() < 60_000;
}

let refreshInFlight: Promise<AuthResponse | null> | null = null;

async function refreshSession(): Promise<AuthResponse | null> {
  const current = getAuth();
  if (!current?.refreshToken) {
    return null;
  }

  if (!refreshInFlight) {
    refreshInFlight = fetch("/api/auth/refresh", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: current.refreshToken })
    })
      .then(async (response) => {
        if (!response.ok) {
          setAuth(null);
          return null;
        }
        const next = (await response.json()) as AuthResponse;
        setAuth(next);
        return next;
      })
      .catch(() => {
        setAuth(null);
        return null;
      })
      .finally(() => {
        refreshInFlight = null;
      });
  }

  return refreshInFlight;
}

async function request<T>(path: string, init?: RequestInit, retry = true): Promise<T> {
  let auth = getAuth();
  if (auth && isExpiringSoon(auth)) {
    auth = (await refreshSession()) ?? auth;
  }

  const headers = new Headers(init?.headers);
  headers.set("Content-Type", "application/json");
  if (auth?.accessToken) {
    headers.set("Authorization", `Bearer ${auth.accessToken}`);
  }

  const response = await fetch(path, { ...init, headers });
  if (response.status === 401 && retry && getAuth()?.refreshToken) {
    const refreshed = await refreshSession();
    if (refreshed) {
      return request<T>(path, init, false);
    }
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  const payload = text ? JSON.parse(text) : null;
  if (!response.ok) {
    throw new Error(payload?.message ?? `Request failed (${response.status})`);
  }
  return payload as T;
}

export const api = {
  login: (email: string, password: string) =>
    request<AuthResponse>("/api/auth/login", { method: "POST", body: JSON.stringify({ email, password }) }),
  dashboard: () => request<Dashboard>("/api/dashboard"),
  orders: (status?: string) => request<Paged<Order>>(`/api/orders${status ? `?status=${status}` : ""}`),
  order: (id: string) => request<Order>(`/api/orders/${id}`),
  createOrder: (body: unknown) => request<Order>("/api/orders", { method: "POST", body: JSON.stringify(body) }),
  confirm: (id: string) => request<Order>(`/api/orders/${id}/confirm`, { method: "POST" }),
  pay: (id: string, paymentReference: string) =>
    request<Order>(`/api/orders/${id}/pay`, { method: "POST", body: JSON.stringify({ paymentReference }) }),
  ship: (id: string, trackingNumber: string) =>
    request<Order>(`/api/orders/${id}/ship`, { method: "POST", body: JSON.stringify({ trackingNumber }) }),
  complete: (id: string) => request<Order>(`/api/orders/${id}/complete`, { method: "POST" }),
  cancel: (id: string, reason: string) =>
    request<Order>(`/api/orders/${id}/cancel`, { method: "POST", body: JSON.stringify({ reason }) }),
  products: (activeOnly = true) =>
    request<Paged<Product>>(`/api/products?pageSize=50${activeOnly ? "&activeOnly=true" : ""}`),
  createProduct: (body: unknown) => request<Product>("/api/products", { method: "POST", body: JSON.stringify(body) }),
  customers: (activeOnly = true) =>
    request<Paged<Customer>>(`/api/customers?pageSize=50${activeOnly ? "&activeOnly=true" : ""}`),
  createCustomer: (body: unknown) => request<Customer>("/api/customers", { method: "POST", body: JSON.stringify(body) })
};
