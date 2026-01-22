"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import * as z from "zod"
import { createClient } from "@/lib/supabase/client"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { toast } from "sonner"

const loginSchema = z.object({
  email: z.string().email("Please enter a valid email"),
  password: z.string().min(8, "Password must be at least 8 characters"),
})

type LoginFormData = z.infer<typeof loginSchema>

export function LoginForm() {
  const [isLoading, setIsLoading] = useState(false)
  const router = useRouter()
  const supabase = createClient()

  const form = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: "",
      password: "",
    },
  })

  async function onSubmit(data: LoginFormData) {
    setIsLoading(true)

    const { error } = await supabase.auth.signInWithPassword({
      email: data.email,
      password: data.password,
    })

    if (error) {
      toast.error("Error signing in", {
        description: error.message,
      })
      setIsLoading(false)
      return
    }

    toast.success("Welcome back, partner! 🤠", {
      description: "Successfully signed in.",
    })

    router.push("/")
    router.refresh()
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="email">Email</Label>
        <Input
          id="email"
          type="email"
          placeholder="partner@blackbartsgold.com"
          {...form.register("email")}
          className="border-saddle-light focus:border-gold focus:ring-gold"
        />
        {form.formState.errors.email && (
          <p className="text-sm text-fire">{form.formState.errors.email.message}</p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="password">Password</Label>
        <Input
          id="password"
          type="password"
          placeholder="••••••••"
          {...form.register("password")}
          className="border-saddle-light focus:border-gold focus:ring-gold"
        />
        {form.formState.errors.password && (
          <p className="text-sm text-fire">{form.formState.errors.password.message}</p>
        )}
      </div>

      <Button
        type="submit"
        className="w-full bg-gold hover:bg-gold-dark text-leather font-semibold"
        disabled={isLoading}
      >
        {isLoading ? "Signing in..." : "Sign In"}
      </Button>
    </form>
  )
}
