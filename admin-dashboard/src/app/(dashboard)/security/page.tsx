import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"

export default function SecurityPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-saddle-dark">Security Monitoring</h2>
        <p className="text-leather-light">
          Detect cheaters and monitor suspicious activity
        </p>
      </div>

      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark">Coming Soon</CardTitle>
          <CardDescription>
            Security monitoring will be implemented in Phase 6
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-leather text-sm">
            This page will include:
          </p>
          <ul className="text-sm text-leather-light list-disc list-inside space-y-1 mt-2">
            <li>Cheater detection alerts</li>
            <li>Velocity anomaly tracking (too many coins too fast)</li>
            <li>GPS spoofing detection</li>
            <li>User suspension/ban tools</li>
            <li>Audit logs</li>
            <li>IP and device tracking</li>
          </ul>
        </CardContent>
      </Card>
    </div>
  )
}
