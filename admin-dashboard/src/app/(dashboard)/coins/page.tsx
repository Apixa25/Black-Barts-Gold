import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"

export default function CoinsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-saddle-dark">Coin Management</h2>
        <p className="text-leather-light">
          View, create, and manage treasure coins
        </p>
      </div>

      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark">Coming Soon</CardTitle>
          <CardDescription>
            Coin management will be implemented in Phase 3
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-leather text-sm">
            This page will include:
          </p>
          <ul className="text-sm text-leather-light list-disc list-inside space-y-1 mt-2">
            <li>Coin list with filters (status, tier, age, value)</li>
            <li>Map visualization of coin locations</li>
            <li>Create/edit/delete coins</li>
            <li>Bulk operations (retrieve stale coins, relocate)</li>
            <li>Mythical coin management</li>
            <li>Sponsor coin tracking</li>
          </ul>
        </CardContent>
      </Card>
    </div>
  )
}
