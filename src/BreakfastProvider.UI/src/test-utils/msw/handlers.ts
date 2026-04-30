import { http, HttpResponse } from 'msw';

const API = 'http://localhost:3000/api';

export const handlers = [
  // --- Pancakes ---
  http.post(`${API}/pancakes`, async () => {
    return HttpResponse.json({
      batchId: 'batch-001',
      ingredients: ['flour', 'eggs', 'milk', 'sugar'],
    });
  }),

  // --- Waffles ---
  http.post(`${API}/waffles`, async () => {
    return HttpResponse.json({
      batchId: 'waffle-001',
      crispiness: 'golden',
    });
  }),

  // --- Orders ---
  http.get(`${API}/orders`, ({ request }) => {
    const url = new URL(request.url);
    const page = Number(url.searchParams.get('page') ?? '1');
    return HttpResponse.json({
      items: [
        { orderId: 'order-1', status: 'Created', itemCount: 3, createdAt: '2024-01-15T10:00:00Z' },
        { orderId: 'order-2', status: 'Preparing', itemCount: 1, createdAt: '2024-01-15T09:30:00Z' },
      ],
      totalCount: 2,
      page,
      pageSize: 10,
    });
  }),

  http.get(`${API}/orders/:id`, ({ params }) => {
    return HttpResponse.json({
      orderId: params.id,
      status: 'Created',
      items: [{ name: 'Pancake', quantity: 2 }],
      createdAt: '2024-01-15T10:00:00Z',
    });
  }),

  http.post(`${API}/orders`, async () => {
    return HttpResponse.json({ orderId: 'new-order-1', status: 'Created' }, { status: 201 });
  }),

  http.patch(`${API}/orders/:id/status`, async () => {
    return HttpResponse.json({ success: true });
  }),

  // --- Menu ---
  http.get(`${API}/menu`, () => {
    return HttpResponse.json({
      items: [
        { id: 'menu-1', name: 'Classic Pancakes', description: 'Fluffy stack', price: 8.99 },
        { id: 'menu-2', name: 'Belgian Waffles', description: 'Crispy waffles', price: 9.99 },
      ],
    });
  }),

  http.delete(`${API}/menu/cache`, () => {
    return new HttpResponse(null, { status: 204 });
  }),

  // --- Toppings ---
  http.get(`${API}/toppings`, () => {
    return HttpResponse.json([
      { id: 'top-1', name: 'Chocolate Chips', category: 'Sweet', price: 1.50, available: true },
      { id: 'top-2', name: 'Bacon Bits', category: 'Savoury', price: 2.00, available: true },
    ]);
  }),

  http.post(`${API}/toppings`, async () => {
    return HttpResponse.json({ id: 'top-3', name: 'New Topping' }, { status: 201 });
  }),

  http.delete(`${API}/toppings/:id`, () => {
    return new HttpResponse(null, { status: 204 });
  }),

  // --- Daily Specials ---
  http.get(`${API}/daily-specials`, () => {
    return HttpResponse.json([
      { id: 'ds-1', name: 'Sunrise Stack', description: 'Limited edition', maxOrders: 50, currentOrders: 30 },
    ]);
  }),

  http.post(`${API}/daily-specials/orders`, () => {
    return HttpResponse.json({ orderId: 'ds-order-1' }, { status: 201 });
  }),

  // --- Ingredients ---
  http.get(`${API}/milk`, () => {
    return HttpResponse.json({ milk: 'whole' });
  }),

  http.get(`${API}/goat-milk`, () => {
    return HttpResponse.json({ goatMilk: 'fresh' });
  }),

  http.get(`${API}/eggs`, () => {
    return HttpResponse.json({ eggs: 'free-range' });
  }),

  http.get(`${API}/flour`, () => {
    return HttpResponse.json({ flour: 'all-purpose' });
  }),

  // --- Inventory ---
  http.get(`${API}/inventory`, () => {
    return HttpResponse.json([
      { id: 1, name: 'Flour', category: 'Dry', quantity: 50, unit: 'kg', reorderLevel: 10, lastRestockedAt: '2024-01-10', createdAt: '2024-01-01' },
    ]);
  }),

  http.post(`${API}/inventory`, () => {
    return HttpResponse.json({ id: 2, name: 'Sugar' }, { status: 201 });
  }),

  http.delete(`${API}/inventory/:id`, () => {
    return new HttpResponse(null, { status: 204 });
  }),

  // --- Reservations ---
  http.get(`${API}/reservations`, () => {
    return HttpResponse.json([
      { id: 1, customerName: 'Alice', tableNumber: 5, partySize: 4, reservedAt: '2024-01-15T18:00:00Z', status: 'Confirmed', createdAt: '2024-01-14' },
    ]);
  }),

  http.post(`${API}/reservations`, () => {
    return HttpResponse.json({ id: 2, customerName: 'Bob' }, { status: 201 });
  }),

  http.delete(`${API}/reservations/:id`, () => {
    return new HttpResponse(null, { status: 204 });
  }),

  // --- Staff ---
  http.get(`${API}/staff`, () => {
    return HttpResponse.json([
      { id: 1, name: 'Chef Alice', role: 'Chef', email: 'alice@breakfast.com', isActive: true, hiredAt: '2023-01-01', createdAt: '2023-01-01' },
    ]);
  }),

  http.post(`${API}/staff`, () => {
    return HttpResponse.json({ id: 2, name: 'Bob' }, { status: 201 });
  }),

  http.delete(`${API}/staff/:id`, () => {
    return new HttpResponse(null, { status: 204 });
  }),

  // --- Audit Logs ---
  http.get(`${API}/audit-logs`, () => {
    return HttpResponse.json([
      { auditLogId: 'log-1', action: 'Created', entityType: 'Order', entityId: 'order-1', details: 'Order created', timestamp: '2024-01-15T10:00:00Z' },
    ]);
  }),

  // --- Health ---
  http.get(`${API}/health`, () => {
    return HttpResponse.json({
      status: 'Healthy',
      entries: [
        { name: 'CowService', status: 'Healthy', duration: '00:00:00.012' },
        { name: 'GoatService', status: 'Degraded', description: 'Slow response' },
        { name: 'CosmosDB', status: 'Healthy' },
      ],
    });
  }),

  // --- Reporting (GraphQL) ---
  http.post(`${API}/graphql`, async ({ request }) => {
    const body = (await request.json()) as { query: string };
    if (body.query.includes('orderSummary')) {
      return HttpResponse.json({
        data: {
          orderSummary: [
            { status: 'Created', count: 10, totalValue: 150.0 },
            { status: 'Completed', count: 25, totalValue: 450.0 },
          ],
        },
      });
    }
    return HttpResponse.json({ data: {} });
  }),

  // --- Server Action Direct Calls ---
  // Server actions (e.g. clearMenuCacheAction) call the API directly without /api prefix
  http.delete('http://localhost:5080/menu/cache', () => {
    return new HttpResponse(null, { status: 204 });
  }),
];
