import { FormEvent, useEffect, useMemo, useState } from "react";
import { NavLink, Navigate, Route, Routes, useNavigate, useParams } from "react-router-dom";
import {
  api,
  AuthResponse,
  Customer,
  Dashboard,
  getAuth,
  Order,
  Product,
  setAuth
} from "./api";

function money(value: number) {
  return value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function LoginPage({ onLogin }: { onLogin: (auth: AuthResponse) => void }) {
  const [email, setEmail] = useState("admin@orderflow.dev");
  const [password, setPassword] = useState("Admin@123");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError("");
    try {
      onLogin(await api.login(email, password));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-wrap">
      <form className="auth-card" onSubmit={submit}>
        <h1>ORDERFLOW</h1>
        <p>Enterprise order management. Sign in to move orders through the lifecycle.</p>
        <label>Email</label>
        <input value={email} onChange={(e) => setEmail(e.target.value)} type="email" required />
        <label>Password</label>
        <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" required />
        {error ? <p className="error">{error}</p> : null}
        <div style={{ marginTop: 20 }}>
          <button className="btn" disabled={loading}>{loading ? "Signing in..." : "Sign in"}</button>
        </div>
        <p className="muted" style={{ marginTop: 18, fontSize: 13 }}>
          Seed users: admin@orderflow.dev / Admin@123 · manager@orderflow.dev / Manager@123 · operator@orderflow.dev / Operator@123
        </p>
      </form>
    </div>
  );
}

function Shell({ auth, onLogout }: { auth: AuthResponse; onLogout: () => void }) {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">ORDERFLOW</div>
        <nav className="nav">
          <NavLink to="/" end>Dashboard</NavLink>
          <NavLink to="/orders">Orders</NavLink>
          <NavLink to="/orders/new">New order</NavLink>
        </nav>
        <div style={{ marginTop: "auto", paddingTop: 24 }} className="muted">
          <div>{auth.fullName}</div>
          <div>{auth.role}</div>
          <button className="btn secondary" style={{ marginTop: 12 }} onClick={onLogout}>Sign out</button>
        </div>
      </aside>
      <main className="content">
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/orders" element={<OrdersPage />} />
          <Route path="/orders/new" element={<NewOrderPage />} />
          <Route path="/orders/:id" element={<OrderDetailPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  );
}

function DashboardPage() {
  const [data, setData] = useState<Dashboard | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    api.dashboard().then(setData).catch((err) => setError(err.message));
  }, []);

  if (error) return <p className="error">{error}</p>;
  if (!data) return <p className="muted">Loading dashboard...</p>;

  const cards = [
    ["Orders", data.totalOrders],
    ["Revenue", money(data.totalRevenue)],
    ["Confirmed", data.ordersByStatus.Confirmed ?? 0],
    ["Completed", data.ordersByStatus.Completed ?? 0]
  ];

  return (
    <>
      <div className="topbar"><h2>Operations dashboard</h2></div>
      <div className="grid">
        {cards.map(([label, value]) => (
          <div className="panel stat" key={String(label)}>
            <span>{label}</span>
            <strong>{value}</strong>
          </div>
        ))}
      </div>
      <div className="panel">
        <h3>Recent orders</h3>
        <OrderTable orders={data.recentOrders} />
      </div>
    </>
  );
}

function OrdersPage() {
  const [status, setStatus] = useState("");
  const [orders, setOrders] = useState<Order[]>([]);
  const [error, setError] = useState("");

  useEffect(() => {
    api.orders(status || undefined).then((p) => setOrders(p.items)).catch((err) => setError(err.message));
  }, [status]);

  return (
    <>
      <div className="topbar">
        <h2>Orders</h2>
        <select value={status} onChange={(e) => setStatus(e.target.value)} style={{ maxWidth: 220 }}>
          <option value="">All statuses</option>
          {["Draft", "Confirmed", "Paid", "Shipped", "Completed", "Cancelled"].map((s) => (
            <option key={s}>{s}</option>
          ))}
        </select>
      </div>
      {error ? <p className="error">{error}</p> : <div className="panel"><OrderTable orders={orders} /></div>}
    </>
  );
}

function OrderTable({ orders }: { orders: Order[] }) {
  return (
    <table className="table">
      <thead>
        <tr>
          <th>Number</th>
          <th>Status</th>
          <th>Total</th>
          <th>Created</th>
        </tr>
      </thead>
      <tbody>
        {orders.map((order) => (
          <tr key={order.id}>
            <td><NavLink to={`/orders/${order.id}`}>{order.orderNumber}</NavLink></td>
            <td><span className={`badge ${order.status}`}>{order.status}</span></td>
            <td>{money(order.totalAmount)}</td>
            <td>{new Date(order.createdAtUtc).toLocaleString("pt-BR")}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function NewOrderPage() {
  const navigate = useNavigate();
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [customerId, setCustomerId] = useState("");
  const [productId, setProductId] = useState("");
  const [quantity, setQuantity] = useState(1);
  const [notes, setNotes] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    Promise.all([api.customers(), api.products()]).then(([c, p]) => {
      setCustomers(c.items);
      setProducts(p.items);
      setCustomerId(c.items[0]?.id ?? "");
      setProductId(p.items[0]?.id ?? "");
    }).catch((err) => setError(err.message));
  }, []);

  async function submit(event: FormEvent) {
    event.preventDefault();
    try {
      const order = await api.createOrder({
        customerId,
        notes,
        items: [{ productId, quantity }]
      });
      navigate(`/orders/${order.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not create order");
    }
  }

  return (
    <>
      <div className="topbar"><h2>New draft order</h2></div>
      <form className="panel" onSubmit={submit}>
        <div className="form-grid">
          <div>
            <label>Customer</label>
            <select value={customerId} onChange={(e) => setCustomerId(e.target.value)}>
              {customers.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </div>
          <div>
            <label>Product</label>
            <select value={productId} onChange={(e) => setProductId(e.target.value)}>
              {products.map((p) => <option key={p.id} value={p.id}>{p.sku} · {p.name} ({p.stockQuantity})</option>)}
            </select>
          </div>
        </div>
        <label>Quantity</label>
        <input type="number" min={1} value={quantity} onChange={(e) => setQuantity(Number(e.target.value))} />
        <label>Notes</label>
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3} />
        {error ? <p className="error">{error}</p> : null}
        <div style={{ marginTop: 16 }}>
          <button className="btn">Create draft</button>
        </div>
      </form>
    </>
  );
}

function OrderDetailPage() {
  const { id } = useParams();
  const [order, setOrder] = useState<Order | null>(null);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [paymentReference, setPaymentReference] = useState("PIX-001");
  const [trackingNumber, setTrackingNumber] = useState("BR123456789");
  const [reason, setReason] = useState("Customer requested cancellation");

  async function reload() {
    if (!id) return;
    setOrder(await api.order(id));
  }

  useEffect(() => {
    reload().catch((err) => setError(err.message));
  }, [id]);

  async function run(action: () => Promise<Order>) {
    setError("");
    try {
      setOrder(await action());
      setMessage("Status updated.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Action failed");
    }
  }

  const next = useMemo(() => {
    if (!order) return null;
    if (order.status === "Draft") return <button className="btn" onClick={() => run(() => api.confirm(order.id))}>Confirm</button>;
    if (order.status === "Confirmed") return <button className="btn" onClick={() => run(() => api.pay(order.id, paymentReference))}>Mark paid</button>;
    if (order.status === "Paid") return <button className="btn" onClick={() => run(() => api.ship(order.id, trackingNumber))}>Ship</button>;
    if (order.status === "Shipped") return <button className="btn" onClick={() => run(() => api.complete(order.id))}>Complete</button>;
    return null;
  }, [order, paymentReference, trackingNumber]);

  if (!order) return error ? <p className="error">{error}</p> : <p className="muted">Loading order...</p>;

  return (
    <>
      <div className="topbar">
        <div>
          <h2>{order.orderNumber}</h2>
          <span className={`badge ${order.status}`}>{order.status}</span>
        </div>
        <strong>{money(order.totalAmount)}</strong>
      </div>
      <div className="panel">
        <div className="form-grid">
          <div>
            <label>Payment reference</label>
            <input value={paymentReference} onChange={(e) => setPaymentReference(e.target.value)} />
          </div>
          <div>
            <label>Tracking number</label>
            <input value={trackingNumber} onChange={(e) => setTrackingNumber(e.target.value)} />
          </div>
        </div>
        <div className="row" style={{ marginTop: 16 }}>
          {next}
          {["Draft", "Confirmed", "Paid"].includes(order.status) ? (
            <button className="btn danger" onClick={() => run(() => api.cancel(order.id, reason))}>Cancel</button>
          ) : null}
        </div>
        <label>Cancellation reason</label>
        <input value={reason} onChange={(e) => setReason(e.target.value)} />
        {message ? <p className="muted">{message}</p> : null}
        {error ? <p className="error">{error}</p> : null}
      </div>
      <div className="panel">
        <h3>Items</h3>
        <table className="table">
          <thead><tr><th>SKU</th><th>Product</th><th>Qty</th><th>Total</th></tr></thead>
          <tbody>
            {order.items.map((item) => (
              <tr key={item.id}>
                <td>{item.sku}</td>
                <td>{item.productName}</td>
                <td>{item.quantity}</td>
                <td>{money(item.lineTotal)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="panel">
        <h3>Status history</h3>
        <table className="table">
          <thead><tr><th>From</th><th>To</th><th>When</th><th>Reason</th></tr></thead>
          <tbody>
            {order.history.map((h, index) => (
              <tr key={index}>
                <td>{h.fromStatus}</td>
                <td>{h.toStatus}</td>
                <td>{new Date(h.changedAtUtc).toLocaleString("pt-BR")}</td>
                <td>{h.reason}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}

export default function App() {
  const [auth, setAuthState] = useState<AuthResponse | null>(getAuth());

  function login(next: AuthResponse) {
    setAuth(next);
    setAuthState(next);
  }

  function logout() {
    setAuth(null);
    setAuthState(null);
  }

  if (!auth) return <LoginPage onLogin={login} />;
  return <Shell auth={auth} onLogout={logout} />;
}
