"use client"

import { useRouter, useSearchParams } from "next/navigation"
import { useState, useTransition } from "react"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Search, X, Download } from "lucide-react"

export function TransactionsSearch() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [isPending, startTransition] = useTransition()
  
  const [type, setType] = useState(searchParams.get("type") || "all")
  const [status, setStatus] = useState(searchParams.get("status") || "all")
  const [dateRange, setDateRange] = useState(searchParams.get("range") || "all")

  const handleSearch = () => {
    startTransition(() => {
      const params = new URLSearchParams()
      if (type && type !== "all") params.set("type", type)
      if (status && status !== "all") params.set("status", status)
      if (dateRange && dateRange !== "all") params.set("range", dateRange)
      
      router.push(`/finances?${params.toString()}`)
    })
  }

  const handleClear = () => {
    setType("all")
    setStatus("all")
    setDateRange("all")
    startTransition(() => {
      router.push("/finances")
    })
  }

  const hasFilters = type !== "all" || status !== "all" || dateRange !== "all"

  return (
    <div className="flex flex-col sm:flex-row gap-3">
      <Select value={type} onValueChange={setType}>
        <SelectTrigger className="w-full sm:w-[160px] border-saddle-light/30">
          <SelectValue placeholder="Type" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Types</SelectItem>
          <SelectItem value="deposit">💵 Deposits</SelectItem>
          <SelectItem value="found">🪙 Coins Found</SelectItem>
          <SelectItem value="hidden">📍 Coins Hidden</SelectItem>
          <SelectItem value="gas_consumed">⛽ Gas Fees</SelectItem>
          <SelectItem value="transfer_in">📥 Received</SelectItem>
          <SelectItem value="transfer_out">📤 Sent</SelectItem>
          <SelectItem value="payout">💸 Payouts</SelectItem>
        </SelectContent>
      </Select>

      <Select value={status} onValueChange={setStatus}>
        <SelectTrigger className="w-full sm:w-[140px] border-saddle-light/30">
          <SelectValue placeholder="Status" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Status</SelectItem>
          <SelectItem value="pending">⏳ Pending</SelectItem>
          <SelectItem value="confirmed">✅ Confirmed</SelectItem>
          <SelectItem value="failed">❌ Failed</SelectItem>
          <SelectItem value="cancelled">🚫 Cancelled</SelectItem>
        </SelectContent>
      </Select>

      <Select value={dateRange} onValueChange={setDateRange}>
        <SelectTrigger className="w-full sm:w-[140px] border-saddle-light/30">
          <SelectValue placeholder="Date Range" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Time</SelectItem>
          <SelectItem value="today">Today</SelectItem>
          <SelectItem value="week">This Week</SelectItem>
          <SelectItem value="month">This Month</SelectItem>
          <SelectItem value="year">This Year</SelectItem>
        </SelectContent>
      </Select>

      <Button 
        onClick={handleSearch}
        disabled={isPending}
        className="bg-gold hover:bg-gold-dark text-leather"
      >
        <Search className="h-4 w-4 mr-2" />
        {isPending ? "..." : "Filter"}
      </Button>

      {hasFilters && (
        <Button 
          variant="outline" 
          onClick={handleClear}
          className="border-saddle-light/30"
        >
          <X className="h-4 w-4 mr-1" />
          Clear
        </Button>
      )}
    </div>
  )
}
