import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"

export default function SettingsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-saddle-dark">Settings</h2>
        <p className="text-leather-light">
          Configure dashboard and system settings
        </p>
      </div>

      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark">Coming Soon</CardTitle>
          <CardDescription>
            Settings will be expanded as features are added
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-leather text-sm">
            This page will include:
          </p>
          <ul className="text-sm text-leather-light list-disc list-inside space-y-1 mt-2">
            <li>Profile settings</li>
            <li>Notification preferences</li>
            <li>System configuration</li>
            <li>Gas pricing settings</li>
            <li>Coin tier thresholds</li>
            <li>API keys management</li>
          </ul>
        </CardContent>
      </Card>
    </div>
  )
}
