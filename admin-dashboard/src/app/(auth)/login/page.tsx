import { LoginForm } from "@/components/forms/login-form"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"

export default function LoginPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-saddle to-parchment p-4">
      <Card className="w-full max-w-md border-saddle-light shadow-xl">
        <CardHeader className="text-center">
          <div className="mx-auto mb-4 text-6xl">🤠</div>
          <CardTitle className="text-2xl font-bold text-saddle-dark">
            Black Bart&apos;s Gold
          </CardTitle>
          <CardDescription className="text-leather-light">
            Admin Dashboard - Sign in to continue
          </CardDescription>
        </CardHeader>
        <CardContent>
          <LoginForm />
        </CardContent>
      </Card>
    </div>
  )
}
