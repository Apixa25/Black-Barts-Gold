"use client"

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  BarChart,
  Bar,
  Legend,
} from "recharts"

interface RevenueChartProps {
  data: {
    date: string
    deposits: number
    gasRevenue: number
    payouts: number
  }[]
}

export function RevenueChart({ data }: RevenueChartProps) {
  // If no data, show placeholder
  if (!data || data.length === 0) {
    return (
      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark">Revenue Overview</CardTitle>
          <CardDescription>Daily revenue breakdown</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="h-[300px] flex items-center justify-center text-leather-light">
            <p>No revenue data yet. Transactions will appear here.</p>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="border-saddle-light/30">
      <CardHeader>
        <CardTitle className="text-saddle-dark">Revenue Overview</CardTitle>
        <CardDescription>Daily deposits, gas fees, and payouts</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="h-[300px]">
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={data}>
              <defs>
                <linearGradient id="colorDeposits" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#22c55e" stopOpacity={0.3}/>
                  <stop offset="95%" stopColor="#22c55e" stopOpacity={0}/>
                </linearGradient>
                <linearGradient id="colorGas" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#FFD700" stopOpacity={0.3}/>
                  <stop offset="95%" stopColor="#FFD700" stopOpacity={0}/>
                </linearGradient>
                <linearGradient id="colorPayouts" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#E25822" stopOpacity={0.3}/>
                  <stop offset="95%" stopColor="#E25822" stopOpacity={0}/>
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#D2B48C40" />
              <XAxis 
                dataKey="date" 
                stroke="#8B4513" 
                fontSize={12}
                tickLine={false}
              />
              <YAxis 
                stroke="#8B4513" 
                fontSize={12}
                tickLine={false}
                tickFormatter={(value) => `$${value}`}
              />
              <Tooltip 
                contentStyle={{ 
                  backgroundColor: '#F5E6D3',
                  border: '1px solid #D2B48C',
                  borderRadius: '8px',
                }}
                formatter={(value) => [`$${Number(value ?? 0).toFixed(2)}`, '']}
              />
              <Legend />
              <Area
                type="monotone"
                dataKey="deposits"
                name="Deposits"
                stroke="#22c55e"
                fillOpacity={1}
                fill="url(#colorDeposits)"
              />
              <Area
                type="monotone"
                dataKey="gasRevenue"
                name="Gas Revenue"
                stroke="#FFD700"
                fillOpacity={1}
                fill="url(#colorGas)"
              />
              <Area
                type="monotone"
                dataKey="payouts"
                name="Payouts"
                stroke="#E25822"
                fillOpacity={1}
                fill="url(#colorPayouts)"
              />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </CardContent>
    </Card>
  )
}

interface TransactionBreakdownProps {
  data: {
    type: string
    count: number
    amount: number
  }[]
}

export function TransactionBreakdown({ data }: TransactionBreakdownProps) {
  if (!data || data.length === 0) {
    return (
      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark">Transaction Breakdown</CardTitle>
          <CardDescription>By type</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="h-[300px] flex items-center justify-center text-leather-light">
            <p>No transaction data yet.</p>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="border-saddle-light/30">
      <CardHeader>
        <CardTitle className="text-saddle-dark">Transaction Breakdown</CardTitle>
        <CardDescription>Volume and amount by type</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="h-[300px]">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={data}>
              <CartesianGrid strokeDasharray="3 3" stroke="#D2B48C40" />
              <XAxis 
                dataKey="type" 
                stroke="#8B4513" 
                fontSize={12}
                tickLine={false}
              />
              <YAxis 
                yAxisId="left"
                stroke="#8B4513" 
                fontSize={12}
                tickLine={false}
              />
              <YAxis 
                yAxisId="right"
                orientation="right"
                stroke="#8B4513" 
                fontSize={12}
                tickLine={false}
                tickFormatter={(value) => `$${value}`}
              />
              <Tooltip 
                contentStyle={{ 
                  backgroundColor: '#F5E6D3',
                  border: '1px solid #D2B48C',
                  borderRadius: '8px',
                }}
              />
              <Legend />
              <Bar 
                yAxisId="left"
                dataKey="count" 
                name="Count"
                fill="#B87333" 
                radius={[4, 4, 0, 0]}
              />
              <Bar 
                yAxisId="right"
                dataKey="amount" 
                name="Amount ($)"
                fill="#FFD700" 
                radius={[4, 4, 0, 0]}
              />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </CardContent>
    </Card>
  )
}
