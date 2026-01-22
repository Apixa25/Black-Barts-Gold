"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { createClient } from "@/lib/supabase/client"
import type { Sponsor, SponsorStatus } from "@/types/database"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Building2, Mail, Phone, Globe, User } from "lucide-react"
import { toast } from "sonner"

interface SponsorDialogProps {
  sponsor?: Sponsor | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

const statusOptions: { value: SponsorStatus; label: string; color: string }[] = [
  { value: "active", label: "Active", color: "bg-green-100 text-green-700" },
  { value: "pending", label: "Pending", color: "bg-yellow-100 text-yellow-700" },
  { value: "inactive", label: "Inactive", color: "bg-gray-100 text-gray-600" },
]

export function SponsorDialog({ sponsor, open, onOpenChange }: SponsorDialogProps) {
  const router = useRouter()
  const supabase = createClient()
  const [isSaving, setIsSaving] = useState(false)
  
  const isEditing = !!sponsor
  
  const [form, setForm] = useState({
    company_name: "",
    contact_name: "",
    contact_email: "",
    contact_phone: "",
    website_url: "",
    logo_url: "",
    description: "",
    status: "pending" as SponsorStatus,
  })

  // Reset form when dialog opens/closes or sponsor changes
  useEffect(() => {
    if (open && sponsor) {
      setForm({
        company_name: sponsor.company_name,
        contact_name: sponsor.contact_name || "",
        contact_email: sponsor.contact_email,
        contact_phone: sponsor.contact_phone || "",
        website_url: sponsor.website_url || "",
        logo_url: sponsor.logo_url || "",
        description: sponsor.description || "",
        status: sponsor.status,
      })
    } else if (open && !sponsor) {
      // Reset to defaults for new sponsor
      setForm({
        company_name: "",
        contact_name: "",
        contact_email: "",
        contact_phone: "",
        website_url: "",
        logo_url: "",
        description: "",
        status: "pending",
      })
    }
  }, [open, sponsor])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setIsSaving(true)

    // Validate required fields
    if (!form.company_name.trim()) {
      toast.error("Company name is required")
      setIsSaving(false)
      return
    }
    if (!form.contact_email.trim()) {
      toast.error("Contact email is required")
      setIsSaving(false)
      return
    }

    const sponsorData = {
      company_name: form.company_name.trim(),
      contact_name: form.contact_name.trim() || null,
      contact_email: form.contact_email.trim(),
      contact_phone: form.contact_phone.trim() || null,
      website_url: form.website_url.trim() || null,
      logo_url: form.logo_url.trim() || null,
      description: form.description.trim() || null,
      status: form.status,
    }

    let error
    if (isEditing && sponsor) {
      const { error: updateError } = await supabase
        .from("sponsors")
        .update(sponsorData)
        .eq("id", sponsor.id)
      error = updateError
    } else {
      const { error: insertError } = await supabase
        .from("sponsors")
        .insert(sponsorData)
      error = insertError
    }

    setIsSaving(false)

    if (error) {
      toast.error(`Failed to ${isEditing ? "update" : "create"} sponsor`, {
        description: error.message,
      })
      return
    }

    toast.success(`Sponsor ${isEditing ? "updated" : "created"}! 🏢`, {
      description: sponsorData.company_name,
    })
    onOpenChange(false)
    router.refresh()
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="text-saddle-dark flex items-center gap-2">
            <Building2 className="h-5 w-5 text-brass" />
            {isEditing ? "Edit Sponsor" : "Add New Sponsor"}
          </DialogTitle>
          <DialogDescription>
            {isEditing ? "Update sponsor details" : "Add a new advertising sponsor"}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-4">
          {/* Company Name */}
          <div className="space-y-2">
            <Label htmlFor="company_name">Company Name *</Label>
            <Input
              id="company_name"
              value={form.company_name}
              onChange={(e) => setForm({ ...form, company_name: e.target.value })}
              placeholder="Acme Corporation"
              className="border-saddle-light/30"
              required
            />
          </div>

          {/* Contact Info Row */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="contact_name" className="flex items-center gap-1">
                <User className="h-3 w-3" />
                Contact Name
              </Label>
              <Input
                id="contact_name"
                value={form.contact_name}
                onChange={(e) => setForm({ ...form, contact_name: e.target.value })}
                placeholder="John Smith"
                className="border-saddle-light/30"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="contact_phone" className="flex items-center gap-1">
                <Phone className="h-3 w-3" />
                Phone
              </Label>
              <Input
                id="contact_phone"
                type="tel"
                value={form.contact_phone}
                onChange={(e) => setForm({ ...form, contact_phone: e.target.value })}
                placeholder="+1 (555) 123-4567"
                className="border-saddle-light/30"
              />
            </div>
          </div>

          {/* Email */}
          <div className="space-y-2">
            <Label htmlFor="contact_email" className="flex items-center gap-1">
              <Mail className="h-3 w-3" />
              Contact Email *
            </Label>
            <Input
              id="contact_email"
              type="email"
              value={form.contact_email}
              onChange={(e) => setForm({ ...form, contact_email: e.target.value })}
              placeholder="sponsor@company.com"
              className="border-saddle-light/30"
              required
            />
          </div>

          {/* Website & Logo */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="website_url" className="flex items-center gap-1">
                <Globe className="h-3 w-3" />
                Website
              </Label>
              <Input
                id="website_url"
                type="url"
                value={form.website_url}
                onChange={(e) => setForm({ ...form, website_url: e.target.value })}
                placeholder="https://company.com"
                className="border-saddle-light/30"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="logo_url">Logo URL</Label>
              <Input
                id="logo_url"
                type="url"
                value={form.logo_url}
                onChange={(e) => setForm({ ...form, logo_url: e.target.value })}
                placeholder="https://..."
                className="border-saddle-light/30"
              />
            </div>
          </div>

          {/* Description */}
          <div className="space-y-2">
            <Label htmlFor="description">Description</Label>
            <Textarea
              id="description"
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              placeholder="Brief description of the sponsor..."
              className="border-saddle-light/30 resize-none"
              rows={2}
            />
          </div>

          {/* Status */}
          <div className="space-y-2">
            <Label>Status</Label>
            <div className="flex gap-2">
              {statusOptions.map((option) => {
                const isSelected = form.status === option.value
                return (
                  <button
                    key={option.value}
                    type="button"
                    onClick={() => setForm({ ...form, status: option.value })}
                    className={`flex-1 px-3 py-2 rounded-lg border-2 transition-colors text-sm font-medium ${
                      isSelected
                        ? "border-gold bg-gold/10 text-saddle-dark"
                        : "border-saddle-light/30 hover:border-saddle-light text-leather"
                    }`}
                  >
                    {option.label}
                  </button>
                )
              })}
            </div>
          </div>
        </form>

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            className="border-saddle-light/30"
          >
            Cancel
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={isSaving}
            className="bg-gold hover:bg-gold-dark text-leather"
          >
            {isSaving ? "Saving..." : isEditing ? "Save Changes" : "Add Sponsor"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
