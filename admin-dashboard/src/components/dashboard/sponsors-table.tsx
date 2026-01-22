"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { createClient } from "@/lib/supabase/client"
import type { Sponsor, SponsorStatus } from "@/types/database"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
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
  Building2,
  CheckCircle,
  XCircle,
  Clock,
  Pencil,
  Trash2,
  ExternalLink,
  Mail,
  Coins
} from "lucide-react"
import { toast } from "sonner"

interface SponsorsTableProps {
  sponsors: Sponsor[]
  onEdit?: (sponsor: Sponsor) => void
}

const statusConfig: Record<SponsorStatus, { label: string; color: string; icon: typeof CheckCircle }> = {
  active: { 
    label: "Active", 
    color: "bg-green-100 text-green-700",
    icon: CheckCircle
  },
  inactive: { 
    label: "Inactive", 
    color: "bg-gray-100 text-gray-600",
    icon: XCircle
  },
  pending: { 
    label: "Pending", 
    color: "bg-yellow-100 text-yellow-700",
    icon: Clock
  },
}

export function SponsorsTable({ sponsors, onEdit }: SponsorsTableProps) {
  const router = useRouter()
  const supabase = createClient()
  const [isDeleting, setIsDeleting] = useState<string | null>(null)

  const handleStatusChange = async (sponsorId: string, newStatus: SponsorStatus) => {
    const { error } = await supabase
      .from("sponsors")
      .update({ status: newStatus })
      .eq("id", sponsorId)

    if (error) {
      toast.error("Failed to update status", {
        description: error.message,
      })
      return
    }

    toast.success("Sponsor status updated! 🏢", {
      description: `Status changed to ${statusConfig[newStatus].label}`,
    })
    router.refresh()
  }

  const handleDelete = async (sponsorId: string) => {
    if (!confirm("Are you sure you want to delete this sponsor? This action cannot be undone.")) {
      return
    }

    setIsDeleting(sponsorId)
    const { error } = await supabase
      .from("sponsors")
      .delete()
      .eq("id", sponsorId)

    setIsDeleting(null)

    if (error) {
      toast.error("Failed to delete sponsor", {
        description: error.message,
      })
      return
    }

    toast.success("Sponsor deleted! 🗑️")
    router.refresh()
  }

  if (sponsors.length === 0) {
    return (
      <div className="text-center py-12 text-leather-light">
        <Building2 className="mx-auto h-12 w-12 text-saddle-light/50 mb-4" />
        <p className="text-lg font-medium">No sponsors yet</p>
        <p className="text-sm">Add your first sponsor to get started!</p>
      </div>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow className="hover:bg-transparent">
          <TableHead className="text-leather">Company</TableHead>
          <TableHead className="text-leather">Contact</TableHead>
          <TableHead className="text-leather">Status</TableHead>
          <TableHead className="text-leather">Coins</TableHead>
          <TableHead className="text-leather">Total Spent</TableHead>
          <TableHead className="text-leather">Joined</TableHead>
          <TableHead className="text-right text-leather">Actions</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {sponsors.map((sponsor) => {
          const status = statusConfig[sponsor.status] || statusConfig.pending
          const StatusIcon = status.icon

          return (
            <TableRow key={sponsor.id} className="hover:bg-parchment/50">
              <TableCell>
                <div className="flex items-center gap-3">
                  <Avatar className="h-9 w-9 border border-saddle-light/30">
                    <AvatarImage src={sponsor.logo_url || undefined} />
                    <AvatarFallback className="bg-brass/20 text-saddle-dark text-sm">
                      {sponsor.company_name.slice(0, 2).toUpperCase()}
                    </AvatarFallback>
                  </Avatar>
                  <div>
                    <p className="font-medium text-saddle-dark">
                      {sponsor.company_name}
                    </p>
                    {sponsor.website_url && (
                      <a 
                        href={sponsor.website_url} 
                        target="_blank" 
                        rel="noopener noreferrer"
                        className="text-xs text-brass hover:underline flex items-center gap-1"
                      >
                        Visit site <ExternalLink className="h-3 w-3" />
                      </a>
                    )}
                  </div>
                </div>
              </TableCell>
              <TableCell>
                <div className="text-sm">
                  <p className="text-saddle-dark">{sponsor.contact_name || '—'}</p>
                  <a 
                    href={`mailto:${sponsor.contact_email}`}
                    className="text-leather-light hover:text-brass flex items-center gap-1"
                  >
                    <Mail className="h-3 w-3" />
                    {sponsor.contact_email}
                  </a>
                </div>
              </TableCell>
              <TableCell>
                <Badge className={`${status.color} gap-1`}>
                  <StatusIcon className="h-3 w-3" />
                  {status.label}
                </Badge>
              </TableCell>
              <TableCell>
                <div className="flex items-center gap-1 text-sm">
                  <Coins className="h-4 w-4 text-gold" />
                  <span className="text-saddle-dark font-medium">{sponsor.coins_purchased}</span>
                  <span className="text-leather-light">purchased</span>
                </div>
              </TableCell>
              <TableCell>
                <span className="font-semibold text-green-600">
                  ${sponsor.total_spent.toFixed(2)}
                </span>
              </TableCell>
              <TableCell className="text-leather-light text-sm">
                {formatDistanceToNow(new Date(sponsor.created_at), { addSuffix: true })}
              </TableCell>
              <TableCell className="text-right">
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button 
                      variant="ghost" 
                      className="h-8 w-8 p-0"
                      disabled={isDeleting === sponsor.id}
                    >
                      <span className="sr-only">Open menu</span>
                      <MoreHorizontal className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end" className="w-48">
                    <DropdownMenuLabel>Actions</DropdownMenuLabel>
                    <DropdownMenuSeparator />
                    {onEdit && (
                      <DropdownMenuItem onClick={() => onEdit(sponsor)}>
                        <Pencil className="mr-2 h-4 w-4" />
                        Edit Sponsor
                      </DropdownMenuItem>
                    )}
                    <DropdownMenuSeparator />
                    <DropdownMenuLabel className="text-xs text-muted-foreground">
                      Change Status
                    </DropdownMenuLabel>
                    <DropdownMenuItem 
                      onClick={() => handleStatusChange(sponsor.id, "active")}
                      disabled={sponsor.status === "active"}
                    >
                      <CheckCircle className="mr-2 h-4 w-4 text-green-600" />
                      Active
                    </DropdownMenuItem>
                    <DropdownMenuItem 
                      onClick={() => handleStatusChange(sponsor.id, "pending")}
                      disabled={sponsor.status === "pending"}
                    >
                      <Clock className="mr-2 h-4 w-4 text-yellow-600" />
                      Pending
                    </DropdownMenuItem>
                    <DropdownMenuItem 
                      onClick={() => handleStatusChange(sponsor.id, "inactive")}
                      disabled={sponsor.status === "inactive"}
                    >
                      <XCircle className="mr-2 h-4 w-4 text-gray-500" />
                      Inactive
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem 
                      onClick={() => handleDelete(sponsor.id)}
                      className="text-fire focus:text-fire"
                    >
                      <Trash2 className="mr-2 h-4 w-4" />
                      Delete Sponsor
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
