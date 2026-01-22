import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"

export default function SponsorsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-saddle-dark">Sponsor Management</h2>
        <p className="text-leather-light">
          Manage sponsor accounts and branded coins
        </p>
      </div>

      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark">Coming Soon</CardTitle>
          <CardDescription>
            Sponsor management will be implemented in Phase 5
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-leather text-sm">
            This page will include:
          </p>
          <ul className="text-sm text-leather-light list-disc list-inside space-y-1 mt-2">
            <li>Sponsor profiles and onboarding</li>
            <li>Sponsor coin purchase tracking</li>
            <li>Analytics per sponsor (coins found, engagement)</li>
            <li>Logo management and approval</li>
            <li>Billing and invoicing</li>
          </ul>
        </CardContent>
      </Card>
    </div>
  )
}
