'use client';

import { useState } from 'react';
import { Tabs } from '@/components/ui/Tabs/Tabs';
import { Card } from '@/components/ui/Card/Card';
import { Button } from '@/components/ui/Button/Button';
import {
  useOrderSummaries,
  useRecipeReports,
  useIngredientUsage,
  usePopularRecipes,
} from '@/hooks/use-reporting';
import { downloadFile, toCSV } from '@/lib/utils';
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
  PieChart, Pie, Cell,
  LineChart, Line,
} from 'recharts';

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884d8'];

/**
 * Reporting page with GraphQL queries → Recharts visualisation.
 *
 * Learning points:
 * - GraphQL queries via graphql-request (not REST)
 * - Recharts renders SVG charts from data arrays
 * - Tab component switches between report types
 * - JSON export of raw data for developer use
 */
export default function ReportingPage() {
  const [activeTab, setActiveTab] = useState('orders');
  const tabs = [
    { id: 'orders', label: 'Order Summary', content: <OrderSummaryTab /> },
    { id: 'recipes', label: 'Recipes', content: <RecipeTab /> },
    { id: 'ingredients', label: 'Ingredient Usage', content: <IngredientUsageTab /> },
    { id: 'types', label: 'Recipe Types', content: <RecipeTypeTab /> },
  ];

  return (
    <div>
      <h2>Reporting</h2>
      <Tabs tabs={tabs} activeTab={activeTab} onTabChange={setActiveTab} />
    </div>
  );
}

function OrderSummaryTab() {
  const { data, isLoading } = useOrderSummaries();

  if (isLoading || !data) return <p>Loading...</p>;

  return (
    <Card variant="outlined">
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <h3>Orders by Status</h3>
        <Button
          variant="ghost"
          size="sm"
          onClick={() =>
            downloadFile(JSON.stringify(data, null, 2), 'order-summary.json', 'application/json')
          }
        >
          Export JSON
        </Button>
      </div>
      <ResponsiveContainer width="100%" height={300}>
        <BarChart data={data.items}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="status" />
          <YAxis />
          <Tooltip />
          <Legend />
          <Bar dataKey="count" fill="#0088FE" />
          <Bar dataKey="totalValue" fill="#00C49F" />
        </BarChart>
      </ResponsiveContainer>
    </Card>
  );
}

function RecipeTab() {
  const { data } = useRecipeReports();

  return (
    <Card variant="outlined">
      <h3>Recipes Logged</h3>
      <ResponsiveContainer width="100%" height={300}>
        <LineChart data={data ?? []}>
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis dataKey="date" />
          <YAxis />
          <Tooltip />
          <Line type="monotone" dataKey="count" stroke="#8884d8" />
        </LineChart>
      </ResponsiveContainer>
    </Card>
  );
}

function IngredientUsageTab() {
  const { data } = useIngredientUsage();

  return (
    <Card variant="outlined">
      <h3>Ingredient Usage</h3>
      <ResponsiveContainer width="100%" height={300}>
        <BarChart data={data ?? []} layout="vertical">
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis type="number" />
          <YAxis type="category" dataKey="name" width={100} />
          <Tooltip />
          <Bar dataKey="quantity" fill="#FFBB28" />
        </BarChart>
      </ResponsiveContainer>
    </Card>
  );
}

function RecipeTypeTab() {
  const { data } = usePopularRecipes();

  return (
    <Card variant="outlined">
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <h3>Recipe Types Distribution</h3>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => {
            if (data) downloadFile(toCSV(data as unknown as Record<string, unknown>[], ['recipeType', 'count']), 'recipe-types.csv', 'text/csv');
          }}
        >
          Export CSV
        </Button>
      </div>
      <ResponsiveContainer width="100%" height={300}>
        <PieChart>
          <Pie data={data ?? []} dataKey="count" nameKey="type" cx="50%" cy="50%" outerRadius={100} label>
            {data?.map((_, index) => (
              <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
            ))}
          </Pie>
          <Tooltip />
          <Legend />
        </PieChart>
      </ResponsiveContainer>
    </Card>
  );
}
