/**
 * Anti-Cheat Panel (M8)
 *
 * @file admin-dashboard/src/components/dashboard/anti-cheat-panel.tsx
 * @description Panel for viewing flagged players, reviewing cheat flags, and taking enforcement actions
 */

"use client"

import { useState } from "react"
import { useAntiCheat } from "@/hooks/use-anti-cheat"
import type { CheatFlag, FlaggedPlayer } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Shield,
  AlertTriangle,
  Ban,
  UserX,
  RefreshCw,
  Loader2,
  Eye,
  CheckCircle,
  XCircle,
  Clock,
  TrendingUp,
  Users,
  Flag,
} from "lucide-react"
import { toast } from "sonner"
import {
  getCheatReasonLabel,
  getCheatReasonDescription,
  getSeverityBadgeColor,
  getSeverityColor,
} from "@/components/maps/anti-cheat-config"
import { formatDistanceToNow } from "date-fns"
import { PlayerFlagDialog } from "./player-flag-dialog"

interface AntiCheatPanelProps {
  className?: string
}

export function AntiCheatPanel({ className = "" }: AntiCheatPanelProps) {
  const {
    flaggedPlayers,
    stats,
    flags,
    loading,
    error,
    refresh,
    reviewFlag,
    takeAction,
    clearFlag,
  } = useAntiCheat()

  const [selectedPlayer, setSelectedPlayer] = useState<FlaggedPlayer | null>(null)
  const [flagDialogOpen, setFlagDialogOpen] = useState(false)
  const [statusFilter, setStatusFilter] = useState<string>("all")
  const [severityFilter, setSeverityFilter] = useState<string>("all")

  // Filter flags
  const filteredFlags = flags.filter(flag => {
    if (statusFilter !== "all" && flag.status !== statusFilter) return false
    if (severityFilter !== "all" && flag.severity !== severityFilter) return false
    return true
  })

  // Filter flagged players
  const filteredPlayers = flaggedPlayers.filter(player => {
    if (statusFilter !== "all") {
      const hasMatchingStatus = player.flags.some(f => f.status === statusFilter)
      if (!hasMatchingStatus) return false
    }
    if (severityFilter !== "all" && player.highest_severity !== severityFilter) return false
    return true
  })

  const handleReviewFlag = async (
    flag: CheatFlag,
    status: CheatFlag['status'],
    action: CheatFlag['action_taken'],
    notes?: string
  ) => {
    await reviewFlag(flag.id, status, action, notes)
    toast.success(`Flag ${status === 'confirmed' ? 'confirmed' : status === 'false_positive' ? 'cleared' : 'updated'}!`)
  }

  const handleTakeAction = async (
    player: FlaggedPlayer,
    action: CheatFlag['action_taken'],
    reason: string
  ) => {
    await takeAction(player.user_id, action, reason)
    toast.success(`Action taken: ${action}`)
    setFlagDialogOpen(false)
  }

  if (error) {
    return (
      <Card className={className}>
        <CardContent className="pt-6">
          <div className="text-center text-red-600">
            <p>Error loading anti-cheat data: {error}</p>
            <Button onClick={refresh} variant="outline" className="mt-4">
              <RefreshCw className="mr-2 h-4 w-4" />
              Retry
            </Button>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className={className}>
      {/* Header */}
      <Card className="border-saddle-light/30">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2 text-saddle-dark">
                <Shield className="h-5 w-5 text-fire" />
                Anti-Cheat Monitoring
              </CardTitle>
              <CardDescription>
                Detect and manage GPS spoofing, impossible speeds, and other cheating behaviors
              </CardDescription>
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={refresh}
              disabled={loading}
            >
              <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
              Refresh
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {/* Loading State */}
          {loading && !stats && (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-8 w-8 animate-spin text-fire" />
            </div>
          )}

          {/* Stats Dashboard */}
          {stats && (
            <>
              {/* Stats Cards */}
              <div className="grid gap-4 md:grid-cols-4 mb-6">
                <Card className="border-saddle-light/30">
                  <CardHeader className="flex flex-row items-center justify-between pb-2">
                    <CardTitle className="text-sm font-medium text-leather-light">
                      Total Flags
                    </CardTitle>
                    <Flag className="h-4 w-4 text-orange-600" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-saddle-dark">
                      {stats.total_flags}
                    </div>
                    <p className="text-xs text-leather-light">
                      {stats.flags_today} today
                    </p>
                  </CardContent>
                </Card>

                <Card className="border-saddle-light/30">
                  <CardHeader className="flex flex-row items-center justify-between pb-2">
                    <CardTitle className="text-sm font-medium text-leather-light">
                      Pending Review
                    </CardTitle>
                    <Clock className="h-4 w-4 text-yellow-600" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-yellow-600">
                      {stats.pending_flags}
                    </div>
                    <p className="text-xs text-leather-light">
                      Awaiting review
                    </p>
                  </CardContent>
                </Card>

                <Card className="border-saddle-light/30">
                  <CardHeader className="flex flex-row items-center justify-between pb-2">
                    <CardTitle className="text-sm font-medium text-leather-light">
                      Confirmed Cheaters
                    </CardTitle>
                    <AlertTriangle className="h-4 w-4 text-red-600" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-red-600">
                      {stats.confirmed_cheaters}
                    </div>
                    <p className="text-xs text-leather-light">
                      {stats.players_banned} banned
                    </p>
                  </CardContent>
                </Card>

                <Card className="border-saddle-light/30">
                  <CardHeader className="flex flex-row items-center justify-between pb-2">
                    <CardTitle className="text-sm font-medium text-leather-light">
                      Detection Rate
                    </CardTitle>
                    <TrendingUp className="h-4 w-4 text-green-600" />
                  </CardHeader>
                  <CardContent>
                    <div className="text-2xl font-bold text-green-600">
                      {stats.detection_rate.toFixed(1)}%
                    </div>
                    <p className="text-xs text-leather-light">
                      Suspicious activity detected
                    </p>
                  </CardContent>
                </Card>
              </div>

              {/* Filters */}
              <div className="flex gap-4 mb-6">
                <Select value={statusFilter} onValueChange={setStatusFilter}>
                  <SelectTrigger className="w-48">
                    <SelectValue placeholder="Filter by status" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Status</SelectItem>
                    <SelectItem value="pending">Pending</SelectItem>
                    <SelectItem value="investigating">Investigating</SelectItem>
                    <SelectItem value="confirmed">Confirmed</SelectItem>
                    <SelectItem value="false_positive">False Positive</SelectItem>
                    <SelectItem value="resolved">Resolved</SelectItem>
                  </SelectContent>
                </Select>

                <Select value={severityFilter} onValueChange={setSeverityFilter}>
                  <SelectTrigger className="w-48">
                    <SelectValue placeholder="Filter by severity" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Severity</SelectItem>
                    <SelectItem value="low">Low</SelectItem>
                    <SelectItem value="medium">Medium</SelectItem>
                    <SelectItem value="high">High</SelectItem>
                    <SelectItem value="critical">Critical</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {/* Flagged Players Table */}
              <Card className="border-saddle-light/30 mb-6">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2 text-saddle-dark">
                    <Users className="h-5 w-5 text-fire" />
                    Flagged Players
                  </CardTitle>
                  <CardDescription>
                    Players with active cheat detection flags
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  {filteredPlayers.length === 0 ? (
                    <div className="text-center py-8 text-leather-light">
                      <Shield className="mx-auto h-12 w-12 text-saddle-light/50 mb-4" />
                      <p className="text-lg font-medium">No flagged players</p>
                      <p className="text-sm">All players are clean! 🎉</p>
                    </div>
                  ) : (
                    <Table>
                      <TableHeader>
                        <TableRow className="hover:bg-transparent">
                          <TableHead className="text-leather">Player</TableHead>
                          <TableHead className="text-leather">Flags</TableHead>
                          <TableHead className="text-leather">Severity</TableHead>
                          <TableHead className="text-leather">Action</TableHead>
                          <TableHead className="text-leather">Last Flag</TableHead>
                          <TableHead className="text-right text-leather">Actions</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {filteredPlayers.map((player) => (
                          <TableRow key={player.user_id} className="hover:bg-parchment/50">
                            <TableCell>
                              <div>
                                <div className="font-medium text-saddle-dark">
                                  {player.user_name}
                                </div>
                                <div className="text-xs text-leather-light">
                                  {player.user_email}
                                </div>
                              </div>
                            </TableCell>
                            <TableCell>
                              <div className="flex items-center gap-2">
                                <Badge variant="outline">
                                  {player.active_flags} active
                                </Badge>
                                <span className="text-xs text-leather-light">
                                  ({player.total_flags} total)
                                </span>
                              </div>
                            </TableCell>
                            <TableCell>
                              <Badge
                                className={`${getSeverityBadgeColor(player.highest_severity)} border`}
                              >
                                {player.highest_severity}
                              </Badge>
                            </TableCell>
                            <TableCell>
                              {player.current_action === 'none' ? (
                                <span className="text-sm text-leather-light">None</span>
                              ) : player.current_action === 'warned' ? (
                                <Badge variant="outline" className="text-yellow-600">
                                  Warned
                                </Badge>
                              ) : player.current_action === 'suspended' ? (
                                <Badge variant="outline" className="text-orange-600">
                                  Suspended
                                </Badge>
                              ) : player.current_action === 'banned' ? (
                                <Badge variant="outline" className="text-red-600">
                                  Banned
                                </Badge>
                              ) : (
                                <Badge variant="outline" className="text-green-600">
                                  Cleared
                                </Badge>
                              )}
                            </TableCell>
                            <TableCell className="text-sm text-leather-light">
                              {player.last_flag_at
                                ? formatDistanceToNow(new Date(player.last_flag_at), {
                                    addSuffix: true,
                                  })
                                : 'Never'}
                            </TableCell>
                            <TableCell className="text-right">
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => {
                                  setSelectedPlayer(player)
                                  setFlagDialogOpen(true)
                                }}
                              >
                                <Eye className="h-4 w-4 mr-2" />
                                Review
                              </Button>
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  )}
                </CardContent>
              </Card>

              {/* Recent Flags Table */}
              <Card className="border-saddle-light/30">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2 text-saddle-dark">
                    <Flag className="h-5 w-5 text-fire" />
                    Recent Flags
                  </CardTitle>
                  <CardDescription>
                    Latest cheat detection flags
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  {filteredFlags.length === 0 ? (
                    <div className="text-center py-8 text-leather-light">
                      <Flag className="mx-auto h-12 w-12 text-saddle-light/50 mb-4" />
                      <p className="text-lg font-medium">No flags found</p>
                      <p className="text-sm">No cheat detection flags match the current filters</p>
                    </div>
                  ) : (
                    <Table>
                      <TableHeader>
                        <TableRow className="hover:bg-transparent">
                          <TableHead className="text-leather">Player</TableHead>
                          <TableHead className="text-leather">Reason</TableHead>
                          <TableHead className="text-leather">Severity</TableHead>
                          <TableHead className="text-leather">Status</TableHead>
                          <TableHead className="text-leather">Detected</TableHead>
                          <TableHead className="text-right text-leather">Actions</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {filteredFlags.map((flag) => {
                          const player = flaggedPlayers.find(p => p.user_id === flag.user_id)
                          return (
                            <TableRow key={flag.id} className="hover:bg-parchment/50">
                              <TableCell>
                                <div className="font-medium text-saddle-dark">
                                  {player?.user_name || 'Unknown'}
                                </div>
                              </TableCell>
                              <TableCell>
                                <div>
                                  <div className="font-medium text-leather">
                                    {getCheatReasonLabel(flag.reason)}
                                  </div>
                                  <div className="text-xs text-leather-light">
                                    {getCheatReasonDescription(flag.reason)}
                                  </div>
                                </div>
                              </TableCell>
                              <TableCell>
                                <Badge
                                  className={`${getSeverityBadgeColor(flag.severity)} border`}
                                >
                                  {flag.severity}
                                </Badge>
                              </TableCell>
                              <TableCell>
                                {flag.status === 'pending' ? (
                                  <Badge variant="outline" className="text-yellow-600">
                                    <Clock className="h-3 w-3 mr-1" />
                                    Pending
                                  </Badge>
                                ) : flag.status === 'investigating' ? (
                                  <Badge variant="outline" className="text-blue-600">
                                    Investigating
                                  </Badge>
                                ) : flag.status === 'confirmed' ? (
                                  <Badge variant="outline" className="text-red-600">
                                    <CheckCircle className="h-3 w-3 mr-1" />
                                    Confirmed
                                  </Badge>
                                ) : flag.status === 'false_positive' ? (
                                  <Badge variant="outline" className="text-green-600">
                                    <XCircle className="h-3 w-3 mr-1" />
                                    False Positive
                                  </Badge>
                                ) : (
                                  <Badge variant="outline">Resolved</Badge>
                                )}
                              </TableCell>
                              <TableCell className="text-sm text-leather-light">
                                {formatDistanceToNow(new Date(flag.detected_at), {
                                  addSuffix: true,
                                })}
                              </TableCell>
                              <TableCell className="text-right">
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => {
                                    if (player) {
                                      setSelectedPlayer(player)
                                      setFlagDialogOpen(true)
                                    }
                                  }}
                                >
                                  <Eye className="h-4 w-4" />
                                </Button>
                              </TableCell>
                            </TableRow>
                          )
                        })}
                      </TableBody>
                    </Table>
                  )}
                </CardContent>
              </Card>
            </>
          )}
        </CardContent>
      </Card>

      {/* Player Flag Dialog */}
      {selectedPlayer && (
        <PlayerFlagDialog
          open={flagDialogOpen}
          onOpenChange={setFlagDialogOpen}
          player={selectedPlayer}
          onReview={handleReviewFlag}
          onTakeAction={handleTakeAction}
          onClear={async (flagId, notes) => {
            await clearFlag(flagId, notes)
            toast.success("Flag cleared!")
          }}
        />
      )}
    </div>
  )
}
