"use client"

import type { Transaction, TransactionType, TransactionStatus } from "@/types/database"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { formatDistanceToNow } from "date-fns"
import { 
  ArrowDownCircle,
  ArrowUpCircle,
  Coins,
  Fuel,
  Send,
  Download,
  Wallet,
  CircleDollarSign
} from "lucide-react"

interface TransactionsTableProps {
  transactions: Transaction[]
}

const typeConfig: Record<TransactionType, { 
  label: string
  color: string
  icon: typeof Coins
  isIncome: boolean
}> = {
  deposit: { 
    label: "Deposit", 
    color: "bg-green-100 text-green-700",
    icon: ArrowDownCircle,
    isIncome: true
  },
  found: { 
    label: "Coin Found", 
    color: "bg-gold/20 text-gold-dark",
    icon: Coins,
    isIncome: true
  },
  hidden: { 
    label: "Coin Hidden", 
    color: "bg-brass/20 text-brass",
    icon: Download,
    isIncome: false
  },
  gas_consumed: { 
    label: "Gas Fee", 
    color: "bg-fire/20 text-fire",
    icon: Fuel,
    isIncome: false
  },
  transfer_in: { 
    label: "Received", 
    color: "bg-blue-100 text-blue-700",
    icon: ArrowDownCircle,
    isIncome: true
  },
  transfer_out: { 
    label: "Sent", 
    color: "bg-purple-100 text-purple-700",
    icon: Send,
    isIncome: false
  },
  payout: { 
    label: "Payout", 
    color: "bg-red-100 text-red-700",
    icon: ArrowUpCircle,
    isIncome: false
  },
}

const statusConfig: Record<TransactionStatus, { label: string; color: string }> = {
  pending: { label: "Pending", color: "bg-yellow-100 text-yellow-700" },
  confirmed: { label: "Confirmed", color: "bg-green-100 text-green-700" },
  failed: { label: "Failed", color: "bg-red-100 text-red-700" },
  cancelled: { label: "Cancelled", color: "bg-gray-100 text-gray-600" },
}

export function TransactionsTable({ transactions }: TransactionsTableProps) {
  if (transactions.length === 0) {
    return (
      <div className="text-center py-12 text-leather-light">
        <CircleDollarSign className="mx-auto h-12 w-12 text-saddle-light/50 mb-4" />
        <p className="text-lg font-medium">No transactions yet</p>
        <p className="text-sm">Transactions will appear here as users interact with the system.</p>
      </div>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow className="hover:bg-transparent">
          <TableHead className="text-leather">Type</TableHead>
          <TableHead className="text-leather">Amount</TableHead>
          <TableHead className="text-leather">Balance After</TableHead>
          <TableHead className="text-leather">Status</TableHead>
          <TableHead className="text-leather">Description</TableHead>
          <TableHead className="text-leather">Date</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {transactions.map((tx) => {
          const type = typeConfig[tx.transaction_type] || typeConfig.deposit
          const status = statusConfig[tx.status] || statusConfig.pending
          const TypeIcon = type.icon

          return (
            <TableRow key={tx.id} className="hover:bg-parchment/50">
              <TableCell>
                <div className="flex items-center gap-2">
                  <div className={`p-1.5 rounded-full ${type.color}`}>
                    <TypeIcon className="h-3.5 w-3.5" />
                  </div>
                  <span className="text-sm font-medium text-saddle-dark">
                    {type.label}
                  </span>
                </div>
              </TableCell>
              <TableCell>
                <span className={`font-semibold ${type.isIncome ? 'text-green-600' : 'text-fire'}`}>
                  {type.isIncome ? '+' : '-'}${tx.amount.toFixed(2)}
                </span>
              </TableCell>
              <TableCell className="text-leather">
                ${tx.balance_after.toFixed(2)}
              </TableCell>
              <TableCell>
                <Badge className={`${status.color} text-xs`}>
                  {status.label}
                </Badge>
              </TableCell>
              <TableCell className="text-leather-light text-sm max-w-[200px] truncate">
                {tx.description || '—'}
              </TableCell>
              <TableCell className="text-leather-light text-sm">
                {formatDistanceToNow(new Date(tx.created_at), { addSuffix: true })}
              </TableCell>
            </TableRow>
          )
        })}
      </TableBody>
    </Table>
  )
}
