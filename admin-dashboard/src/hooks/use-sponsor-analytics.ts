/**
 * Sponsor Analytics Hook
 * Phase M7: Sponsor Features
 * 
 * Provides sponsor analytics data and actions for managing sponsor zones,
 * viewing performance metrics, and bulk coin placement.
 */

"use client"

import { useState, useEffect } from "react"
import type { 
  SponsorAnalytics, 
  SponsorZoneAnalytics, 
  Sponsor,
  BulkCoinPlacementConfig 
} from "@/types/database"
import { calculatePerformanceScore } from "@/components/maps/sponsor-config"

// ============================================================================
// MOCK DATA GENERATION
// ============================================================================

/**
 * Generate mock sponsor analytics
 */
function generateMockSponsorAnalytics(sponsorId: string, sponsorName: string): SponsorAnalytics {
  const zoneAnalytics: SponsorZoneAnalytics[] = [
    {
      zone_id: 'zone-1',
      zone_name: 'Downtown Storefront',
      sponsor_id: sponsorId,
      sponsor_name: sponsorName,
      total_coins_placed: 50,
      coins_collected: 38,
      coins_active: 8,
      coins_expired: 4,
      total_value_placed: 125.00,
      total_value_collected: 95.00,
      average_coin_value: 2.50,
      unique_collectors: 24,
      total_collections: 38,
      average_collection_time_minutes: 45,
      first_coin_placed_at: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
      last_coin_collected_at: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
      peak_collection_hour: 14, // 2 PM
      performance_score: calculatePerformanceScore(50, 38, 125.00, 95.00, 24, 7),
    },
    {
      zone_id: 'zone-2',
      zone_name: 'Shopping Mall Entrance',
      sponsor_id: sponsorId,
      sponsor_name: sponsorName,
      total_coins_placed: 30,
      coins_collected: 22,
      coins_active: 5,
      coins_expired: 3,
      total_value_placed: 75.00,
      total_value_collected: 55.00,
      average_coin_value: 2.50,
      unique_collectors: 18,
      total_collections: 22,
      average_collection_time_minutes: 32,
      first_coin_placed_at: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
      last_coin_collected_at: new Date(Date.now() - 1 * 60 * 60 * 1000).toISOString(),
      peak_collection_hour: 18, // 6 PM
      performance_score: calculatePerformanceScore(30, 22, 75.00, 55.00, 18, 5),
    },
  ]
  
  const totalCoinsPlaced = zoneAnalytics.reduce((sum, z) => sum + z.total_coins_placed, 0)
  const totalCoinsCollected = zoneAnalytics.reduce((sum, z) => sum + z.coins_collected, 0)
  const totalValuePlaced = zoneAnalytics.reduce((sum, z) => sum + z.total_value_placed, 0)
  const totalValueCollected = zoneAnalytics.reduce((sum, z) => sum + z.total_value_collected, 0)
  const totalUniqueCollectors = new Set(
    zoneAnalytics.flatMap(z => Array.from({ length: z.unique_collectors }, (_, i) => `${z.zone_id}-${i}`))
  ).size
  
  return {
    sponsor_id: sponsorId,
    sponsor_name: sponsorName,
    total_zones: zoneAnalytics.length,
    active_zones: zoneAnalytics.length,
    total_zone_area_km2: 0.5,
    total_coins_placed: totalCoinsPlaced,
    total_coins_collected: totalCoinsCollected,
    total_coins_active: zoneAnalytics.reduce((sum, z) => sum + z.coins_active, 0),
    collection_rate: totalCoinsPlaced > 0 ? (totalCoinsCollected / totalCoinsPlaced) * 100 : 0,
    total_value_placed: totalValuePlaced,
    total_value_collected: totalValueCollected,
    average_coin_value: totalCoinsPlaced > 0 ? totalValuePlaced / totalCoinsPlaced : 0,
    roi_percentage: totalValuePlaced > 0 
      ? ((totalValueCollected - totalValuePlaced) / totalValuePlaced) * 100 
      : 0,
    total_unique_collectors: totalUniqueCollectors,
    total_collections: zoneAnalytics.reduce((sum, z) => sum + z.total_collections, 0),
    average_collections_per_zone: zoneAnalytics.length > 0 
      ? totalCoinsCollected / zoneAnalytics.length 
      : 0,
    total_spent: 250.00,
    cost_per_collection: totalCoinsCollected > 0 ? 250.00 / totalCoinsCollected : 250.00,
    cost_per_unique_collector: totalUniqueCollectors > 0 ? 250.00 / totalUniqueCollectors : 250.00,
    period_start: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
    period_end: new Date().toISOString(),
    zone_analytics: zoneAnalytics,
  }
}

/**
 * Generate mock sponsors list
 */
function generateMockSponsors(): Sponsor[] {
  return [
    {
      id: 'sponsor-1',
      company_name: 'TechCorp Solutions',
      contact_name: 'John Smith',
      contact_email: 'john@techcorp.com',
      contact_phone: '+1-555-0101',
      logo_url: null,
      website_url: 'https://techcorp.com',
      description: 'Leading technology solutions provider',
      total_spent: 250.00,
      coins_purchased: 80,
      coins_collected: 60,
      status: 'active',
      admin_user_id: null,
      created_at: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
      updated_at: new Date().toISOString(),
    },
    {
      id: 'sponsor-2',
      company_name: 'Local Coffee Shop',
      contact_name: 'Sarah Johnson',
      contact_email: 'sarah@coffee.com',
      contact_phone: '+1-555-0102',
      logo_url: null,
      website_url: 'https://localcoffee.com',
      description: 'Neighborhood coffee shop',
      total_spent: 150.00,
      coins_purchased: 50,
      coins_collected: 35,
      status: 'active',
      admin_user_id: null,
      created_at: new Date(Date.now() - 20 * 24 * 60 * 60 * 1000).toISOString(),
      updated_at: new Date().toISOString(),
    },
    {
      id: 'sponsor-3',
      company_name: 'Fitness Center',
      contact_name: 'Mike Davis',
      contact_email: 'mike@fitness.com',
      contact_phone: '+1-555-0103',
      logo_url: null,
      website_url: 'https://fitness.com',
      description: 'Premium fitness and wellness center',
      total_spent: 0,
      coins_purchased: 0,
      coins_collected: 0,
      status: 'pending',
      admin_user_id: null,
      created_at: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
      updated_at: new Date().toISOString(),
    },
  ]
}

// ============================================================================
// HOOK
// ============================================================================

interface UseSponsorAnalyticsReturn {
  // Data
  sponsors: Sponsor[]
  analytics: SponsorAnalytics | null
  loading: boolean
  error: string | null
  
  // Actions
  fetchAnalytics: (sponsorId: string) => Promise<void>
  refresh: () => Promise<void>
  placeBulkCoins: (config: BulkCoinPlacementConfig) => Promise<{ success: boolean; message: string }>
}

export function useSponsorAnalytics(): UseSponsorAnalyticsReturn {
  const [sponsors, setSponsors] = useState<Sponsor[]>([])
  const [analytics, setAnalytics] = useState<SponsorAnalytics | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [selectedSponsorId, setSelectedSponsorId] = useState<string | null>(null)

  // Fetch sponsors list
  const fetchSponsors = async () => {
    setLoading(true)
    setError(null)
    
    try {
      // TODO: Replace with actual Supabase call
      // const { data, error } = await supabase.from('sponsors').select('*')
      // if (error) throw error
      
      // Mock data for now
      await new Promise(resolve => setTimeout(resolve, 500)) // Simulate API call
      const mockSponsors = generateMockSponsors()
      setSponsors(mockSponsors)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch sponsors')
    } finally {
      setLoading(false)
    }
  }

  // Fetch analytics for a specific sponsor
  const fetchAnalytics = async (sponsorId: string) => {
    setLoading(true)
    setError(null)
    setSelectedSponsorId(sponsorId)
    
    try {
      // TODO: Replace with actual Supabase call
      // const { data, error } = await supabase
      //   .from('sponsor_analytics')
      //   .select('*')
      //   .eq('sponsor_id', sponsorId)
      //   .single()
      
      // Mock data for now
      await new Promise(resolve => setTimeout(resolve, 500)) // Simulate API call
      const sponsor = sponsors.find(s => s.id === sponsorId)
      if (!sponsor) {
        throw new Error('Sponsor not found')
      }
      
      const mockAnalytics = generateMockSponsorAnalytics(sponsorId, sponsor.company_name)
      setAnalytics(mockAnalytics)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch analytics')
      setAnalytics(null)
    } finally {
      setLoading(false)
    }
  }

  // Refresh data
  const refresh = async () => {
    if (selectedSponsorId) {
      await fetchAnalytics(selectedSponsorId)
    } else {
      await fetchSponsors()
    }
  }

  // Place bulk coins
  const placeBulkCoins = async (
    config: BulkCoinPlacementConfig
  ): Promise<{ success: boolean; message: string }> => {
    setLoading(true)
    setError(null)
    
    try {
      // TODO: Replace with actual Supabase call
      // const { data, error } = await supabase
      //   .rpc('place_bulk_coins', { config })
      // if (error) throw error
      
      // Mock implementation
      await new Promise(resolve => setTimeout(resolve, 1000)) // Simulate API call
      
      return {
        success: true,
        message: `Successfully placed ${config.coin_count} coins!`,
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to place coins'
      setError(message)
      return {
        success: false,
        message,
      }
    } finally {
      setLoading(false)
    }
  }

  // Initial fetch
  useEffect(() => {
    fetchSponsors()
  }, [])

  return {
    sponsors,
    analytics,
    loading,
    error,
    fetchAnalytics,
    refresh,
    placeBulkCoins,
  }
}
