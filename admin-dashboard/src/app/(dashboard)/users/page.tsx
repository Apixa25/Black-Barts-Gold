import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"

export default function UsersPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-saddle-dark">User Management</h2>
        <p className="text-leather-light">
          Manage users, roles, and permissions
        </p>
      </div>

      <Card className="border-saddle-light/30">
        <CardHeader>
          <CardTitle className="text-saddle-dark">Coming Soon</CardTitle>
          <CardDescription>
            User management will be implemented in Phase 2
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-leather text-sm">
            This page will include:
          </p>
          <ul className="text-sm text-leather-light list-disc list-inside space-y-1 mt-2">
            <li>User list with search and filters</li>
            <li>Role management (super_admin, sponsor_admin, user)</li>
            <li>User profile editing</li>
            <li>Account suspension/ban tools</li>
            <li>Gas balance and BBG balance overview</li>
          </ul>
        </CardContent>
      </Card>
    </div>
  )
}
