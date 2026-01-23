/**
 * Player Flag Dialog (M8)
 *
 * @file admin-dashboard/src/components/dashboard/player-flag-dialog.tsx
 * @description Dialog for reviewing cheat flags and taking enforcement actions
 */

"use client"

import { useState } from "react"
import type { FlaggedPlayer, CheatFlag, CheatFlagStatus, PlayerAction } from "@/types/database"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Textarea } from "@/components/ui/textarea"
import { Label } from "@/components/ui/label"
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
  Loader2,
  MapPin,
  Gauge,
  Clock,
  Smartphone,
  CheckCircle,
  XCircle,
} from "lucide-react"
import {
  getCheatReasonLabel,
  getCheatReasonDescription,
  getSeverityBadgeColor,
  calculateSpeed,
} from "@/components/maps/anti-cheat-config"
import { calculateDistance } from "@/components/maps/map-config"
import { format } from "date-fns"

interface PlayerFlagDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  player: FlaggedPlayer
  onReview: (
    flag: CheatFlag,
    status: CheatFlagStatus,
    action: PlayerAction,
    notes?: string
  ) => Promise<void>
  onTakeAction: (
    player: FlaggedPlayer,
    action: PlayerAction,
    reason: string
  ) => Promise<void>
  onClear: (flagId: string, notes: string) => Promise<void>
}

export function PlayerFlagDialog({
  open,
  onOpenChange,
  player,
  onReview,
  onTakeAction,
  onClear,
}: PlayerFlagDialogProps) {
  const [loading, setLoading] = useState(false)
  const [selectedFlag, setSelectedFlag] = useState<CheatFlag | null>(
    player.flags[0] || null
  )
  const [reviewStatus, setReviewStatus] = useState<CheatFlagStatus>("pending")
  const [reviewAction, setReviewAction] = useState<PlayerAction>("none")
  const [notes, setNotes] = useState("")

  const handleReview = async () => {
    if (!selectedFlag) return

    setLoading(true)
    try {
      await onReview(selectedFlag, reviewStatus, reviewAction, notes)
      onOpenChange(false)
      setNotes("")
    } catch (error) {
      console.error("Failed to review flag:", error)
    } finally {
      setLoading(false)
    }
  }

  const handleTakeAction = async (action: PlayerAction) => {
    setLoading(true)
    try {
      await onTakeAction(player, action, notes || `Action: ${action}`)
      onOpenChange(false)
      setNotes("")
    } catch (error) {
      console.error("Failed to take action:", error)
    } finally {
      setLoading(false)
    }
  }

  const handleClear = async () => {
    if (!selectedFlag) return

    setLoading(true)
    try {
      await onClear(selectedFlag.id, notes || "False positive - cleared by admin")
      onOpenChange(false)
      setNotes("")
    } catch (error) {
      console.error("Failed to clear flag:", error)
    } finally {
      setLoading(false)
    }
  }

  const activeFlag = selectedFlag || player.flags[0]

  if (!activeFlag) {
    return null
  }

  // Calculate evidence details
  const evidence = activeFlag.evidence
  const distance = evidence.previous_location && evidence.current_location
    ? calculateDistance(
        evidence.previous_location.latitude,
        evidence.previous_location.longitude,
        evidence.current_location.latitude,
        evidence.current_location.longitude
      )
    : null
  const timeSeconds = evidence.previous_location && evidence.current_location
    ? (new Date(evidence.current_location.timestamp).getTime() -
        new Date(evidence.previous_location.timestamp).getTime()) /
      1000
    : null
  const calculatedSpeed = distance && timeSeconds && timeSeconds > 0
    ? calculateSpeed(distance, timeSeconds)
    : null

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5 text-fire" />
            Review Cheat Flag
          </DialogTitle>
          <DialogDescription>
            Review flag for {player.user_name} ({player.user_email})
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4">
          {/* Player Info */}
          <div className="p-4 bg-parchment/50 rounded-lg">
            <div className="flex items-center justify-between">
              <div>
                <div className="font-medium text-saddle-dark">{player.user_name}</div>
                <div className="text-sm text-leather-light">{player.user_email}</div>
              </div>
              <div className="flex items-center gap-2">
                <Badge
                  className={`${getSeverityBadgeColor(player.highest_severity)} border`}
                >
                  {player.highest_severity}
                </Badge>
                {player.current_action !== 'none' && (
                  <Badge variant="outline">
                    {player.current_action}
                  </Badge>
                )}
              </div>
            </div>
          </div>

          {/* Flag Selection */}
          {player.flags.length > 1 && (
            <div className="space-y-2">
              <Label>Select Flag</Label>
              <Select
                value={activeFlag.id}
                onValueChange={(value) => {
                  const flag = player.flags.find(f => f.id === value)
                  if (flag) {
                    setSelectedFlag(flag)
                    setReviewStatus(flag.status)
                    setReviewAction(flag.action_taken)
                    setNotes(flag.notes || "")
                  }
                }}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {player.flags.map((flag) => (
                    <SelectItem key={flag.id} value={flag.id}>
                      <div className="flex items-center gap-2">
                        <span>{getCheatReasonLabel(flag.reason)}</span>
                        <Badge
                          className={`${getSeverityBadgeColor(flag.severity)} text-xs`}
                        >
                          {flag.severity}
                        </Badge>
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          {/* Flag Details */}
          <div className="space-y-4">
            <div>
              <Label>Flag Reason</Label>
              <div className="mt-1 p-3 bg-parchment/50 rounded-lg">
                <div className="font-medium text-saddle-dark">
                  {getCheatReasonLabel(activeFlag.reason)}
                </div>
                <div className="text-sm text-leather-light mt-1">
                  {getCheatReasonDescription(activeFlag.reason)}
                </div>
              </div>
            </div>

            {/* Evidence */}
            <div className="space-y-3">
              <Label>Evidence</Label>
              
              {evidence.previous_location && evidence.current_location && (
                <div className="p-4 bg-parchment/50 rounded-lg space-y-3">
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <div className="text-xs text-leather-light mb-1">Previous Location</div>
                      <div className="flex items-center gap-2">
                        <MapPin className="h-4 w-4 text-saddle-light" />
                        <span className="text-sm text-leather">
                          {evidence.previous_location.latitude.toFixed(6)},{" "}
                          {evidence.previous_location.longitude.toFixed(6)}
                        </span>
                      </div>
                      <div className="text-xs text-leather-light mt-1">
                        {format(new Date(evidence.previous_location.timestamp), "PPp")}
                      </div>
                    </div>
                    <div>
                      <div className="text-xs text-leather-light mb-1">Current Location</div>
                      <div className="flex items-center gap-2">
                        <MapPin className="h-4 w-4 text-saddle-light" />
                        <span className="text-sm text-leather">
                          {evidence.current_location.latitude.toFixed(6)},{" "}
                          {evidence.current_location.longitude.toFixed(6)}
                        </span>
                      </div>
                      <div className="text-xs text-leather-light mt-1">
                        {format(new Date(evidence.current_location.timestamp), "PPp")}
                      </div>
                    </div>
                  </div>

                  {distance !== null && timeSeconds !== null && calculatedSpeed !== null && (
                    <div className="grid grid-cols-3 gap-4 pt-3 border-t border-saddle-light/30">
                      <div>
                        <div className="text-xs text-leather-light mb-1">Distance</div>
                        <div className="flex items-center gap-2">
                          <MapPin className="h-4 w-4 text-blue-600" />
                          <span className="font-medium text-leather">
                            {(distance / 1000).toFixed(2)} km
                          </span>
                        </div>
                      </div>
                      <div>
                        <div className="text-xs text-leather-light mb-1">Time</div>
                        <div className="flex items-center gap-2">
                          <Clock className="h-4 w-4 text-orange-600" />
                          <span className="font-medium text-leather">
                            {timeSeconds.toFixed(1)}s
                          </span>
                        </div>
                      </div>
                      <div>
                        <div className="text-xs text-leather-light mb-1">Calculated Speed</div>
                        <div className="flex items-center gap-2">
                          <Gauge className="h-4 w-4 text-red-600" />
                          <span className="font-medium text-red-600">
                            {calculatedSpeed.toFixed(0)} km/h
                          </span>
                        </div>
                      </div>
                    </div>
                  )}
                </div>
              )}

              {/* Device Info */}
              <div className="p-4 bg-parchment/50 rounded-lg space-y-2">
                <div className="flex items-center gap-2 mb-2">
                  <Smartphone className="h-4 w-4 text-saddle-light" />
                  <span className="font-medium text-leather">Device Information</span>
                </div>
                <div className="grid grid-cols-2 gap-4 text-sm">
                  {evidence.device_model && (
                    <div>
                      <span className="text-leather-light">Model:</span>{" "}
                      <span className="text-leather">{evidence.device_model}</span>
                    </div>
                  )}
                  {evidence.device_id && (
                    <div>
                      <span className="text-leather-light">Device ID:</span>{" "}
                      <span className="text-leather font-mono text-xs">
                        {evidence.device_id}
                      </span>
                    </div>
                  )}
                  {evidence.is_mock_location !== undefined && (
                    <div>
                      <span className="text-leather-light">Mock Location:</span>{" "}
                      <Badge
                        variant={evidence.is_mock_location ? "destructive" : "outline"}
                        className="ml-2"
                      >
                        {evidence.is_mock_location ? "Yes" : "No"}
                      </Badge>
                    </div>
                  )}
                  {evidence.is_rooted !== undefined && (
                    <div>
                      <span className="text-leather-light">Rooted/Jailbroken:</span>{" "}
                      <Badge
                        variant={evidence.is_rooted ? "destructive" : "outline"}
                        className="ml-2"
                      >
                        {evidence.is_rooted ? "Yes" : "No"}
                      </Badge>
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* Review Form */}
            <div className="space-y-4 border-t pt-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>Review Status</Label>
                  <Select
                    value={reviewStatus}
                    onValueChange={(value: CheatFlagStatus) => setReviewStatus(value)}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="pending">Pending</SelectItem>
                      <SelectItem value="investigating">Investigating</SelectItem>
                      <SelectItem value="confirmed">Confirmed</SelectItem>
                      <SelectItem value="false_positive">False Positive</SelectItem>
                      <SelectItem value="resolved">Resolved</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label>Action to Take</Label>
                  <Select
                    value={reviewAction}
                    onValueChange={(value: PlayerAction) => setReviewAction(value)}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="none">None</SelectItem>
                      <SelectItem value="warned">Warn</SelectItem>
                      <SelectItem value="suspended">Suspend</SelectItem>
                      <SelectItem value="banned">Ban</SelectItem>
                      <SelectItem value="cleared">Clear</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="space-y-2">
                <Label>Notes</Label>
                <Textarea
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  placeholder="Add review notes..."
                  rows={3}
                />
              </div>
            </div>
          </div>
        </div>

        <DialogFooter className="flex items-center justify-between">
          <div className="flex gap-2">
            <Button
              variant="outline"
              onClick={() => handleClear()}
              disabled={loading || activeFlag.status === 'false_positive'}
            >
              <XCircle className="mr-2 h-4 w-4" />
              Clear Flag
            </Button>
            <Button
              variant="destructive"
              onClick={() => handleTakeAction('banned')}
              disabled={loading || player.current_action === 'banned'}
            >
              <Ban className="mr-2 h-4 w-4" />
              Ban Player
            </Button>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => onOpenChange(false)} disabled={loading}>
              Cancel
            </Button>
            <Button onClick={handleReview} disabled={loading}>
              {loading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Saving...
                </>
              ) : (
                <>
                  <CheckCircle className="mr-2 h-4 w-4" />
                  Save Review
                </>
              )}
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
