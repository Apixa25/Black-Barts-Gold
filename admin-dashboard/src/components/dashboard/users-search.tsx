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
import { Search, X } from "lucide-react"

export function UsersSearch() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [isPending, startTransition] = useTransition()
  
  const [search, setSearch] = useState(searchParams.get("search") || "")
  const [role, setRole] = useState(searchParams.get("role") || "all")

  const handleSearch = () => {
    startTransition(() => {
      const params = new URLSearchParams()
      if (search) params.set("search", search)
      if (role && role !== "all") params.set("role", role)
      
      router.push(`/users?${params.toString()}`)
    })
  }

  const handleClear = () => {
    setSearch("")
    setRole("all")
    startTransition(() => {
      router.push("/users")
    })
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") {
      handleSearch()
    }
  }

  return (
    <div className="flex flex-col sm:flex-row gap-3">
      <div className="relative flex-1">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-leather-light" />
        <Input
          placeholder="Search by name or email..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onKeyDown={handleKeyDown}
          className="pl-9 border-saddle-light/30"
        />
      </div>
      
      <Select value={role} onValueChange={setRole}>
        <SelectTrigger className="w-full sm:w-[160px] border-saddle-light/30">
          <SelectValue placeholder="Filter by role" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">All Roles</SelectItem>
          <SelectItem value="super_admin">Super Admin</SelectItem>
          <SelectItem value="sponsor_admin">Sponsor Admin</SelectItem>
          <SelectItem value="user">User</SelectItem>
        </SelectContent>
      </Select>

      <Button 
        onClick={handleSearch}
        disabled={isPending}
        className="bg-gold hover:bg-gold-dark text-leather"
      >
        {isPending ? "Searching..." : "Search"}
      </Button>

      {(search || role !== "all") && (
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
