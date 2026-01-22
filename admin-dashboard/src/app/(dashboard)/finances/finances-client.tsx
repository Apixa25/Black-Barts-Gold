"use client"

import { Suspense } from "react"
import type { Transaction } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { TransactionsTable } from "@/components/dashboard/transactions-table"
import { TransactionsSearch } from "@/components/dashboard/transactions-search"
import { RevenueChart, TransactionBreakdown } from "@/components/dashboard/revenue-chart"

interface FinancesPageClientProps {
  transactions: Transaction[]
  chartData: {
    date: string
    deposits: number
    gasRevenue: number
    payouts: number
  }[]
  breakdownData: {
    type: string
    count: number
    amount: number
  }[]
  error?: string
  hasFilters: boolean
  totalTransactions: number
  searchParams: { type?: string; status?: string; range?: string }
}

export function FinancesPageClient({ 
  transactions, 
  chartData,
  breakdownData,
  error, 
  hasFilters, 
  totalTransactions,
  searchParams 
}: FinancesPageClientProps) {
  return (
    <>
      {/* Charts Row */}
      <div className="grid gap-6 md:grid-cols-2">
        <RevenueChart data={chartData} />
        <TransactionBreakdown data={breakdownData} />
      </div>

      {/* Search and Filter */}
      <Suspense fallback={<div className="h-10 bg-parchment animate-pulse rounded" />}>
        <TransactionsSearch />
      </Suspense>

      {/* Results info */}
      {hasFilters && (
        <p className="text-sm text-leather-light">
          Showing {transactions.length} of {totalTransactions} transactions
          {searchParams.type && searchParams.type !== "all" && (
            <span> of type &quot;{searchParams.type}&quot;</span>
          )}
          {searchParams.status && searchParams.status !== "all" && (
            <span> with status &quot;{searchParams.status}&quot;</span>
          )}
          {searchParams.range && searchParams.range !== "all" && (
            <span> from {searchParams.range}</span>
          )}
        </p>
      )}

      {/* Transactions Table */}
      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark">
            {hasFilters ? "Filtered Transactions" : "Recent Transactions"}
          </CardTitle>
          <CardDescription>
            {transactions.length} {hasFilters ? "matching" : "most recent"} transactions
          </CardDescription>
        </CardHeader>
        <CardContent>
          {error ? (
            <div className="text-fire text-sm p-4 bg-fire/10 rounded-lg">
              Error loading transactions: {error}
            </div>
          ) : (
            <TransactionsTable transactions={transactions} />
          )}
        </CardContent>
      </Card>
    </>
  )
}
