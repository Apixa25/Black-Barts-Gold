"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { createClient } from "@/lib/supabase/client"
import type { Coin, CoinStatus, CoinTier } from "@/types/database"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { formatDistanceToNow } from "date-fns"
import { 
  MoreHorizontal, 
  Coins, 
  MapPin, 
  Eye, 
  EyeOff, 
  CheckCircle,
  XCircle,
  Sparkles,
  Pencil,
  Trash2
} from "lucide-react"
import { toast } from "sonner"

interface CoinsTableProps {
  coins: Coin[]
  onEdit?: (coin: Coin) => void
}

const statusConfig: Record<CoinStatus, { label: string; color: string; icon: typeof Eye }> = {
  hidden: { 
    label: "Hidden", 
    color: "bg-leather-light/30 text-leather",
    icon: EyeOff
  },
  visible: { 
    label: "Visible", 
    color: "bg-gold/20 text-gold-dark",
    icon: Eye
  },
  collected: { 
    label: "Collected", 
    color: "bg-green-100 text-green-700",
    icon: CheckCircle
  },
  expired: { 
    label: "Expired", 
    color: "bg-fire/20 text-fire",
    icon: XCircle
  },
  recycled: { 
    label: "Recycled", 
    color: "bg-brass/20 text-brass",
    icon: Coins
  },
}

const tierConfig: Record<CoinTier, { label: string; color: string; emoji: string }> = {
  gold: { label: "Gold", color: "bg-gold text-leather", emoji: "🥇" },
  silver: { label: "Silver", color: "bg-gray-300 text-gray-700", emoji: "🥈" },
  bronze: { label: "Bronze", color: "bg-orange-200 text-orange-800", emoji: "🥉" },
}

export function CoinsTable({ coins, onEdit }: CoinsTableProps) {
  const router = useRouter()
  const supabase = createClient()
  const [isDeleting, setIsDeleting] = useState<string | null>(null)

  const handleStatusChange = async (coinId: string, newStatus: CoinStatus) => {
    const { error } = await supabase
      .from("coins")
      .update({ status: newStatus })
      .eq("id", coinId)

    if (error) {
      toast.error("Failed to update status", {
        description: error.message,
      })
      return
    }

    toast.success("Coin status updated! 🪙", {
      description: `Status changed to ${statusConfig[newStatus].label}`,
    })
    router.refresh()
  }

  const handleDelete = async (coinId: string) => {
    if (!confirm("Are you sure you want to delete this coin? This action cannot be undone.")) {
      return
    }

    setIsDeleting(coinId)
    const { error } = await supabase
      .from("coins")
      .delete()
      .eq("id", coinId)

    setIsDeleting(null)

    if (error) {
      toast.error("Failed to delete coin", {
        description: error.message,
      })
      return
    }

    toast.success("Coin deleted! 🗑️")
    router.refresh()
  }

  if (coins.length === 0) {
    return (
      <div className="text-center py-12 text-leather-light">
        <Coins className="mx-auto h-12 w-12 text-saddle-light/50 mb-4" />
        <p className="text-lg font-medium">No coins found</p>
        <p className="text-sm">Create your first coin to get started!</p>
      </div>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow className="hover:bg-transparent">
          <TableHead className="text-leather">Value</TableHead>
          <TableHead className="text-leather">Type</TableHead>
          <TableHead className="text-leather">Location</TableHead>
          <TableHead className="text-leather">Status</TableHead>
          <TableHead className="text-leather">Created</TableHead>
          <TableHead className="text-right text-leather">Actions</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {coins.map((coin) => {
          const status = statusConfig[coin.status] || statusConfig.hidden
          const tier = tierConfig[coin.tier] || tierConfig.gold
          const StatusIcon = status.icon

          return (
            <TableRow key={coin.id} className="hover:bg-parchment/50">
              <TableCell>
                <div className="flex items-center gap-2">
                  <Badge className={`${tier.color} gap-1`}>
                    <span>{tier.emoji}</span>
                    ${coin.value.toFixed(2)}
                  </Badge>
                  {coin.is_mythical && (
                    <span title="Mythical Coin">
                      <Sparkles className="h-4 w-4 text-gold" />
                    </span>
                  )}
                </div>
              </TableCell>
              <TableCell>
                <span className="text-sm text-leather capitalize">
                  {coin.coin_type === 'pool' ? '🎰 Pool' : '🎯 Fixed'}
                </span>
              </TableCell>
              <TableCell>
                <div className="flex items-center gap-1 text-sm">
                  <MapPin className="h-3 w-3 text-saddle-light" />
                  {coin.location_name ? (
                    <span className="text-saddle-dark">{coin.location_name}</span>
                  ) : (
                    <span className="text-leather-light">
                      {coin.latitude.toFixed(4)}, {coin.longitude.toFixed(4)}
                    </span>
                  )}
                </div>
              </TableCell>
              <TableCell>
                <Badge className={`${status.color} gap-1`}>
                  <StatusIcon className="h-3 w-3" />
                  {status.label}
                </Badge>
              </TableCell>
              <TableCell className="text-leather-light text-sm">
                {formatDistanceToNow(new Date(coin.created_at), { addSuffix: true })}
              </TableCell>
              <TableCell className="text-right">
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button 
                      variant="ghost" 
                      className="h-8 w-8 p-0"
                      disabled={isDeleting === coin.id}
                    >
                      <span className="sr-only">Open menu</span>
                      <MoreHorizontal className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end" className="w-48">
                    <DropdownMenuLabel>Actions</DropdownMenuLabel>
                    <DropdownMenuSeparator />
                    {onEdit && (
                      <DropdownMenuItem onClick={() => onEdit(coin)}>
                        <Pencil className="mr-2 h-4 w-4" />
                        Edit Coin
                      </DropdownMenuItem>
                    )}
                    <DropdownMenuSeparator />
                    <DropdownMenuLabel className="text-xs text-muted-foreground">
                      Change Status
                    </DropdownMenuLabel>
                    <DropdownMenuItem 
                      onClick={() => handleStatusChange(coin.id, "hidden")}
                      disabled={coin.status === "hidden"}
                    >
                      <EyeOff className="mr-2 h-4 w-4" />
                      Hidden
                    </DropdownMenuItem>
                    <DropdownMenuItem 
                      onClick={() => handleStatusChange(coin.id, "visible")}
                      disabled={coin.status === "visible"}
                    >
                      <Eye className="mr-2 h-4 w-4 text-gold" />
                      Visible
                    </DropdownMenuItem>
                    <DropdownMenuItem 
                      onClick={() => handleStatusChange(coin.id, "expired")}
                      disabled={coin.status === "expired"}
                    >
                      <XCircle className="mr-2 h-4 w-4 text-fire" />
                      Expired
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem 
                      onClick={() => handleDelete(coin.id)}
                      className="text-fire focus:text-fire"
                    >
                      <Trash2 className="mr-2 h-4 w-4" />
                      Delete Coin
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </TableCell>
            </TableRow>
          )
        })}
      </TableBody>
    </Table>
  )
}
