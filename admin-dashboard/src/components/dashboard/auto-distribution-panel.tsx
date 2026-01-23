/**
 * Auto-Distribution Panel Component
 * 
 * @file admin-dashboard/src/components/dashboard/auto-distribution-panel.tsx
 * @description Control panel for managing automatic coin distribution
 * 
 * Character count: ~12,000
 */

"use client"

import { useState } from "react"
import { useAutoDistribution } from "@/hooks/use-auto-distribution"
import type { ZoneDistributionStatus, SpawnQueueItem } from "@/types/database"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Progress } from "@/components/ui/progress"
import { Switch } from "@/components/ui/switch"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { 
  Coins, 
  Play, 
  Pause, 
  Square, 
  RefreshCw,
  Settings,
  Zap,
  Clock,
  TrendingUp,
  AlertTriangle,
  CheckCircle,
  XCircle,
  MoreVertical,
  Plus,
  Recycle,
  Loader2,
  MapPin,
} from "lucide-react"
import { toast } from "sonner"
import { formatTimeUntilSpawn } from "@/components/maps/distribution-config"

interface AutoDistributionPanelProps {
  className?: string
  compact?: boolean
}

export function AutoDistributionPanel({ 
  className = "",
  compact = false 
}: AutoDistributionPanelProps) {
  const {
    stats,
    zoneStatuses,
    spawnQueue,
    config,
    isSpawning,
    error,
    dispatch,
    spawnCoinsForZone,
    refresh,
  } = useAutoDistribution()
  
  const [spawnDialogOpen, setSpawnDialogOpen] = useState(false)
  const [selectedZone, setSelectedZone] = useState<ZoneDistributionStatus | null>(null)
  const [spawnCount, setSpawnCount] = useState(3)
  
  // Status indicator component
  const StatusIndicator = () => {
    const statusConfig = {
      running: { color: 'bg-green-500', label: 'Running', icon: Play },
      paused: { color: 'bg-yellow-500', label: 'Paused', icon: Pause },
      stopped: { color: 'bg-gray-500', label: 'Stopped', icon: Square },
      error: { color: 'bg-red-500', label: 'Error', icon: AlertTriangle },
    }
    const status = statusConfig[stats.system_status]
    const Icon = status.icon
    
    return (
      <Badge variant="outline" className={`${status.color === 'bg-green-500' ? 'text-green-600 border-green-600' : status.color === 'bg-yellow-500' ? 'text-yellow-600 border-yellow-600' : status.color === 'bg-red-500' ? 'text-red-600 border-red-600' : 'text-gray-600 border-gray-600'}`}>
        <Icon className="w-3 h-3 mr-1" />
        {status.label}
      </Badge>
    )
  }
  
  // Handle spawn action
  const handleSpawn = async () => {
    if (!selectedZone) return
    
    try {
      const results = await spawnCoinsForZone(selectedZone.zone_id, spawnCount)
      const successful = results.filter(r => r.success).length
      
      toast.success(`Spawned ${successful} coins`, {
        description: `in ${selectedZone.zone_name}`,
      })
      
      setSpawnDialogOpen(false)
    } catch (err) {
      toast.error('Failed to spawn coins')
    }
  }
  
  // Open spawn dialog for a zone
  const openSpawnDialog = (zone: ZoneDistributionStatus) => {
    setSelectedZone(zone)
    setSpawnCount(zone.coins_to_spawn || 3)
    setSpawnDialogOpen(true)
  }
  
  if (compact) {
    // Compact version for sidebar/header
    return (
      <div className={`flex items-center gap-4 ${className}`}>
        <StatusIndicator />
        <div className="text-sm">
          <span className="font-medium">{stats.coins_spawned_today}</span>
          <span className="text-leather-light ml-1">spawned today</span>
        </div>
        {stats.zones_needing_spawn > 0 && (
          <Badge variant="outline" className="text-fire border-fire">
            {stats.zones_needing_spawn} zones need coins
          </Badge>
        )}
      </div>
    )
  }
  
  return (
    <div className={`space-y-4 ${className}`}>
      {/* Header with controls */}
      <Card className="border-saddle-light/30">
        <CardHeader className="pb-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-lg bg-gold/10">
                <Zap className="h-5 w-5 text-gold" />
              </div>
              <div>
                <CardTitle className="text-saddle-dark flex items-center gap-2">
                  Auto-Distribution
                  <StatusIndicator />
                </CardTitle>
                <CardDescription>
                  Automatic coin spawning and management
                </CardDescription>
              </div>
            </div>
            
            {/* Control buttons */}
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={refresh}
                disabled={isSpawning}
              >
                <RefreshCw className={`h-4 w-4 ${isSpawning ? 'animate-spin' : ''}`} />
              </Button>
              
              {stats.system_status === 'running' ? (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => dispatch({ type: 'pause' })}
                  className="text-yellow-600 border-yellow-600 hover:bg-yellow-50"
                >
                  <Pause className="h-4 w-4 mr-1" />
                  Pause
                </Button>
              ) : (
                <Button
                  size="sm"
                  onClick={() => dispatch({ type: 'start' })}
                  className="bg-green-600 hover:bg-green-700 text-white"
                >
                  <Play className="h-4 w-4 mr-1" />
                  Start
                </Button>
              )}
            </div>
          </div>
        </CardHeader>
        
        <CardContent>
          {/* Stats grid */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-2">
            <div className="text-center p-3 rounded-lg bg-parchment">
              <div className="text-2xl font-bold text-gold">{stats.coins_spawned_today}</div>
              <div className="text-xs text-leather-light">Spawned Today</div>
            </div>
            <div className="text-center p-3 rounded-lg bg-parchment">
              <div className="text-2xl font-bold text-green-600">{stats.coins_collected_today}</div>
              <div className="text-xs text-leather-light">Collected Today</div>
            </div>
            <div className="text-center p-3 rounded-lg bg-parchment">
              <div className="text-2xl font-bold text-blue-600">{stats.queue_length}</div>
              <div className="text-xs text-leather-light">In Queue</div>
            </div>
            <div className="text-center p-3 rounded-lg bg-parchment">
              <div className="text-2xl font-bold text-fire">{stats.zones_needing_spawn}</div>
              <div className="text-xs text-leather-light">Zones Need Coins</div>
            </div>
          </div>
          
          {/* Value stats */}
          <div className="flex items-center justify-between mt-4 text-sm">
            <div className="flex items-center gap-4">
              <span className="text-leather-light">
                💰 Value spawned: <strong className="text-gold">${stats.total_value_spawned_today.toFixed(2)}</strong>
              </span>
              <span className="text-leather-light">
                📊 Avg value: <strong>${stats.average_coin_value.toFixed(2)}</strong>
              </span>
            </div>
            <div className="flex items-center gap-2 text-leather-light">
              <Clock className="h-4 w-4" />
              Next spawn: {stats.next_scheduled_spawn ? formatTimeUntilSpawn(stats.next_scheduled_spawn) : 'N/A'}
            </div>
          </div>
        </CardContent>
      </Card>
      
      {/* Zone Status Table */}
      <Card className="border-saddle-light/30">
        <CardHeader className="pb-2">
          <CardTitle className="text-saddle-dark text-lg flex items-center gap-2">
            <MapPin className="h-5 w-5 text-gold" />
            Zone Distribution Status
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Zone</TableHead>
                <TableHead className="text-center">Auto-Spawn</TableHead>
                <TableHead className="text-center">Coins</TableHead>
                <TableHead className="text-center">Status</TableHead>
                <TableHead className="text-center">Collected Today</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {zoneStatuses.map(zone => (
                <TableRow key={zone.zone_id}>
                  <TableCell>
                    <div>
                      <div className="font-medium text-saddle-dark">{zone.zone_name}</div>
                      <div className="text-xs text-leather-light capitalize">{zone.zone_type} zone</div>
                    </div>
                  </TableCell>
                  <TableCell className="text-center">
                    <Switch
                      checked={zone.auto_spawn_enabled}
                      onCheckedChange={(checked) => {
                        // TODO: Update zone config
                        toast.info(`Auto-spawn ${checked ? 'enabled' : 'disabled'}`)
                      }}
                    />
                  </TableCell>
                  <TableCell className="text-center">
                    <div className="flex flex-col items-center gap-1">
                      <span className="font-medium">
                        {zone.current_coin_count} / {zone.max_coins}
                      </span>
                      <Progress 
                        value={(zone.current_coin_count / zone.max_coins) * 100} 
                        className="h-1.5 w-16"
                      />
                    </div>
                  </TableCell>
                  <TableCell className="text-center">
                    {zone.needs_spawn ? (
                      <Badge variant="outline" className="text-fire border-fire bg-fire/5">
                        <AlertTriangle className="h-3 w-3 mr-1" />
                        Needs {zone.coins_to_spawn}
                      </Badge>
                    ) : (
                      <Badge variant="outline" className="text-green-600 border-green-600 bg-green-50">
                        <CheckCircle className="h-3 w-3 mr-1" />
                        OK
                      </Badge>
                    )}
                  </TableCell>
                  <TableCell className="text-center">
                    <span className="font-medium text-green-600">{zone.collected_today}</span>
                  </TableCell>
                  <TableCell className="text-right">
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                          <MoreVertical className="h-4 w-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onClick={() => openSpawnDialog(zone)}>
                          <Plus className="h-4 w-4 mr-2" />
                          Spawn Coins Now
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => dispatch({ type: 'recycle_stale', zone_id: zone.zone_id })}>
                          <Recycle className="h-4 w-4 mr-2" />
                          Recycle Stale Coins
                        </DropdownMenuItem>
                        <DropdownMenuItem>
                          <Settings className="h-4 w-4 mr-2" />
                          Configure Zone
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
      
      {/* Spawn Queue */}
      {spawnQueue.length > 0 && (
        <Card className="border-saddle-light/30">
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <CardTitle className="text-saddle-dark text-lg flex items-center gap-2">
                <Clock className="h-5 w-5 text-gold" />
                Spawn Queue ({spawnQueue.length})
              </CardTitle>
              <Button
                variant="outline"
                size="sm"
                onClick={() => dispatch({ type: 'clear_queue' })}
                className="text-red-600 border-red-600 hover:bg-red-50"
              >
                Clear Queue
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {spawnQueue.slice(0, 5).map(item => (
                <div 
                  key={item.id}
                  className="flex items-center justify-between p-3 rounded-lg bg-parchment"
                >
                  <div className="flex items-center gap-3">
                    <div className={`w-2 h-2 rounded-full ${
                      item.status === 'processing' ? 'bg-yellow-500 animate-pulse' :
                      item.status === 'pending' ? 'bg-blue-500' :
                      item.status === 'completed' ? 'bg-green-500' : 'bg-red-500'
                    }`} />
                    <div>
                      <div className="text-sm font-medium">{item.zone_name}</div>
                      <div className="text-xs text-leather-light">
                        {item.coin_config.tier} • ${item.coin_config.min_value.toFixed(2)}-${item.coin_config.max_value.toFixed(2)}
                      </div>
                    </div>
                  </div>
                  <div className="text-right">
                    <Badge variant="outline" className="text-xs">
                      {item.trigger_type}
                    </Badge>
                    <div className="text-xs text-leather-light mt-1">
                      {formatTimeUntilSpawn(item.scheduled_time)}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
      
      {/* Spawn Dialog */}
      <Dialog open={spawnDialogOpen} onOpenChange={setSpawnDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Spawn Coins</DialogTitle>
            <DialogDescription>
              Manually spawn coins in {selectedZone?.zone_name}
            </DialogDescription>
          </DialogHeader>
          
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="spawn-count">Number of Coins</Label>
              <Input
                id="spawn-count"
                type="number"
                min={1}
                max={50}
                value={spawnCount}
                onChange={(e) => setSpawnCount(parseInt(e.target.value) || 1)}
              />
            </div>
            
            {selectedZone && (
              <div className="p-3 rounded-lg bg-parchment text-sm">
                <div className="flex justify-between mb-1">
                  <span className="text-leather-light">Current coins:</span>
                  <span className="font-medium">{selectedZone.current_coin_count}</span>
                </div>
                <div className="flex justify-between mb-1">
                  <span className="text-leather-light">After spawn:</span>
                  <span className="font-medium">{selectedZone.current_coin_count + spawnCount}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-leather-light">Zone max:</span>
                  <span className="font-medium">{selectedZone.max_coins}</span>
                </div>
              </div>
            )}
          </div>
          
          <DialogFooter>
            <Button variant="outline" onClick={() => setSpawnDialogOpen(false)}>
              Cancel
            </Button>
            <Button 
              onClick={handleSpawn}
              disabled={isSpawning}
              className="bg-gold hover:bg-gold-dark text-leather"
            >
              {isSpawning ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Spawning...
                </>
              ) : (
                <>
                  <Coins className="h-4 w-4 mr-2" />
                  Spawn {spawnCount} Coins
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      
      {/* Error display */}
      {error && (
        <Card className="border-red-300 bg-red-50">
          <CardContent className="py-3">
            <div className="flex items-center gap-2 text-red-600">
              <XCircle className="h-4 w-4" />
              <span className="text-sm">{error}</span>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

/**
 * Compact status widget for headers/sidebars
 */
export function DistributionStatusWidget() {
  const { stats } = useAutoDistribution()
  
  return (
    <div className="flex items-center gap-2 text-sm">
      <div className={`w-2 h-2 rounded-full ${
        stats.system_status === 'running' ? 'bg-green-500 animate-pulse' :
        stats.system_status === 'paused' ? 'bg-yellow-500' : 'bg-gray-500'
      }`} />
      <span className="font-medium">{stats.coins_spawned_today}</span>
      <span className="text-leather-light">spawned</span>
      {stats.zones_needing_spawn > 0 && (
        <Badge variant="outline" className="text-fire border-fire text-xs">
          {stats.zones_needing_spawn} need coins
        </Badge>
      )}
    </div>
  )
}
