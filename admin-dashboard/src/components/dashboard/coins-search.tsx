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
import { Search, X, Plus } from "lucide-react"

interface CoinsSearchProps {
  onCreateClick: () => void
}

export function CoinsSearch({ onCreateClick }: CoinsSearchProps) {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [isPending, startTransition] = useTransition()
  
  const [search, setSearch] = useState(searchParams.get("search") || "")
  const [status, setStatus] = useState(searchParams.get("status") || "all")
  const [tier, setTier] = useState(searchParams.get("tier") || "all")

  const handleSearch = () => {
    startTransition(() => {
      const params = new URLSearchParams()
      if (search) params.set("search", search)
      if (status && status !== "all") params.set("status", status)
      if (tier && tier !== "all") params.set("tier", tier)
      
      router.push(`/coins?${params.toString()}`)
    })
  }

  const handleClear = () => {
    setSearch("")
    setStatus("all")
    setTier("all")
    startTransition(() => {
      router.push("/coins")
    })
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") {
      handleSearch()
    }
  }

  const hasFilters = search || status !== "all" || tier !== "all"

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-leather-light" />
          <Input
            placeholder="Search by location..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onKeyDown={handleKeyDown}
            className="pl-9 border-saddle-light/30"
          />
        </div>
        
        <Select value={status} onValueChange={setStatus}>
          <SelectTrigger className="w-full sm:w-[140px] border-saddle-light/30">
            <SelectValue placeholder="Status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Status</SelectItem>
            <SelectItem value="hidden">Hidden</SelectItem>
            <SelectItem value="visible">Visible</SelectItem>
            <SelectItem value="collected">Collected</SelectItem>
            <SelectItem value="expired">Expired</SelectItem>
          </SelectContent>
        </Select>

        <Select value={tier} onValueChange={setTier}>
          <SelectTrigger className="w-full sm:w-[130px] border-saddle-light/30">
            <SelectValue placeholder="Tier" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Tiers</SelectItem>
            <SelectItem value="gold">🥇 Gold</SelectItem>
            <SelectItem value="silver">🥈 Silver</SelectItem>
            <SelectItem value="bronze">🥉 Bronze</SelectItem>
          </SelectContent>
        </Select>

        <Button 
          onClick={handleSearch}
          disabled={isPending}
          className="bg-gold hover:bg-gold-dark text-leather"
        >
          {isPending ? "..." : "Search"}
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

      <div className="flex justify-end">
        <Button 
          onClick={onCreateClick}
          className="bg-saddle hover:bg-saddle-dark text-white"
        >
          <Plus className="h-4 w-4 mr-2" />
          Create Coin
        </Button>
      </div>
    </div>
  )
}
