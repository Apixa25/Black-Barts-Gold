import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"

export default function FinancesPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-saddle-dark">Financial Dashboard</h2>
        <p className="text-leather-light">
          Track deposits, payouts, and revenue
        </p>
      </div>

      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark">Coming Soon</CardTitle>
          <CardDescription>
            Financial dashboard will be implemented in Phase 4
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-leather text-sm">
            This page will include:
          </p>
          <ul className="text-sm text-leather-light list-disc list-inside space-y-1 mt-2">
            <li>Total deposits vs payouts overview</li>
            <li>Gas revenue tracking</li>
            <li>Transaction history with filters</li>
            <li>Integrity checks (deposits vs coins in system)</li>
            <li>Charts and visualizations</li>
            <li>Export reports</li>
          </ul>
        </CardContent>
      </Card>
    </div>
  )
}
