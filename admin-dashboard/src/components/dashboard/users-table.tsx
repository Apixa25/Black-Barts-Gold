"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { createClient } from "@/lib/supabase/client"
import type { UserProfile, UserRole } from "@/types/database"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { formatDistanceToNow } from "date-fns"
import { MoreHorizontal, Shield, User, Building2, Pencil } from "lucide-react"
import { toast } from "sonner"

interface UsersTableProps {
  users: UserProfile[]
}

const roleConfig = {
  super_admin: { 
    label: "Super Admin", 
    color: "bg-gold text-leather",
    icon: Shield,
    description: "Full access to all features"
  },
  sponsor_admin: { 
    label: "Sponsor Admin", 
    color: "bg-brass text-white",
    icon: Building2,
    description: "Manage sponsored coins and campaigns"
  },
  user: { 
    label: "User", 
    color: "bg-saddle-light/30 text-saddle-dark",
    icon: User,
    description: "Standard user account"
  },
}

export function UsersTable({ users }: UsersTableProps) {
  const router = useRouter()
  const supabase = createClient()
  const [editingUser, setEditingUser] = useState<UserProfile | null>(null)
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [editForm, setEditForm] = useState({
    full_name: "",
    role: "" as UserRole,
  })

  const openEditDialog = (user: UserProfile) => {
    setEditingUser(user)
    setEditForm({
      full_name: user.full_name || "",
      role: user.role,
    })
    setIsEditDialogOpen(true)
  }

  const handleRoleChange = async (userId: string, newRole: UserRole) => {
    const { error } = await supabase
      .from("profiles")
      .update({ role: newRole })
      .eq("id", userId)

    if (error) {
      toast.error("Failed to update role", {
        description: error.message,
      })
      return
    }

    toast.success("Role updated! 🤠", {
      description: `User role changed to ${roleConfig[newRole].label}`,
    })
    router.refresh()
  }

  const handleSaveEdit = async () => {
    if (!editingUser) return
    setIsSaving(true)

    const { error } = await supabase
      .from("profiles")
      .update({
        full_name: editForm.full_name || null,
        role: editForm.role,
      })
      .eq("id", editingUser.id)

    setIsSaving(false)

    if (error) {
      toast.error("Failed to update user", {
        description: error.message,
      })
      return
    }

    toast.success("User updated! 🤠", {
      description: "Changes saved successfully",
    })
    setIsEditDialogOpen(false)
    router.refresh()
  }

  if (users.length === 0) {
    return (
      <div className="text-center py-12 text-leather-light">
        <User className="mx-auto h-12 w-12 text-saddle-light/50 mb-4" />
        <p className="text-lg font-medium">No users found</p>
        <p className="text-sm">Users will appear here once they sign up.</p>
      </div>
    )
  }

  return (
    <>
      <Table>
        <TableHeader>
          <TableRow className="hover:bg-transparent">
            <TableHead className="text-leather">User</TableHead>
            <TableHead className="text-leather">Role</TableHead>
            <TableHead className="text-leather">Joined</TableHead>
            <TableHead className="text-right text-leather">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {users.map((user) => {
            const role = roleConfig[user.role] || roleConfig.user
            const RoleIcon = role.icon

            return (
              <TableRow key={user.id} className="hover:bg-parchment/50">
                <TableCell>
                  <div className="flex items-center gap-3">
                    <Avatar className="h-9 w-9 border border-saddle-light/30">
                      <AvatarImage src={user.avatar_url || undefined} />
                      <AvatarFallback className="bg-gold/20 text-saddle-dark text-sm">
                        {user.full_name?.[0] || user.email[0].toUpperCase()}
                      </AvatarFallback>
                    </Avatar>
                    <div>
                      <p className="font-medium text-saddle-dark">
                        {user.full_name || "No name set"}
                      </p>
                      <p className="text-sm text-leather-light">{user.email}</p>
                    </div>
                  </div>
                </TableCell>
                <TableCell>
                  <Badge className={`${role.color} gap-1`}>
                    <RoleIcon className="h-3 w-3" />
                    {role.label}
                  </Badge>
                </TableCell>
                <TableCell className="text-leather-light">
                  {formatDistanceToNow(new Date(user.created_at), { addSuffix: true })}
                </TableCell>
                <TableCell className="text-right">
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" className="h-8 w-8 p-0">
                        <span className="sr-only">Open menu</span>
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end" className="w-48">
                      <DropdownMenuLabel>Actions</DropdownMenuLabel>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem onClick={() => openEditDialog(user)}>
                        <Pencil className="mr-2 h-4 w-4" />
                        Edit User
                      </DropdownMenuItem>
                      <DropdownMenuSeparator />
                      <DropdownMenuLabel className="text-xs text-muted-foreground">
                        Change Role
                      </DropdownMenuLabel>
                      <DropdownMenuItem 
                        onClick={() => handleRoleChange(user.id, "super_admin")}
                        disabled={user.role === "super_admin"}
                      >
                        <Shield className="mr-2 h-4 w-4 text-gold" />
                        Super Admin
                      </DropdownMenuItem>
                      <DropdownMenuItem 
                        onClick={() => handleRoleChange(user.id, "sponsor_admin")}
                        disabled={user.role === "sponsor_admin"}
                      >
                        <Building2 className="mr-2 h-4 w-4 text-brass" />
                        Sponsor Admin
                      </DropdownMenuItem>
                      <DropdownMenuItem 
                        onClick={() => handleRoleChange(user.id, "user")}
                        disabled={user.role === "user"}
                      >
                        <User className="mr-2 h-4 w-4" />
                        Regular User
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>

      {/* Edit User Dialog */}
      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="text-saddle-dark">Edit User</DialogTitle>
            <DialogDescription>
              Update user information and role
            </DialogDescription>
          </DialogHeader>
          {editingUser && (
            <div className="space-y-4 py-4">
              <div className="flex items-center gap-3 p-3 bg-parchment rounded-lg">
                <Avatar className="h-10 w-10">
                  <AvatarFallback className="bg-gold/20 text-saddle-dark">
                    {editingUser.full_name?.[0] || editingUser.email[0].toUpperCase()}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <p className="font-medium text-saddle-dark">{editingUser.email}</p>
                  <p className="text-xs text-leather-light">
                    Joined {formatDistanceToNow(new Date(editingUser.created_at), { addSuffix: true })}
                  </p>
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="full_name">Full Name</Label>
                <Input
                  id="full_name"
                  value={editForm.full_name}
                  onChange={(e) => setEditForm({ ...editForm, full_name: e.target.value })}
                  placeholder="Enter full name"
                  className="border-saddle-light/30"
                />
              </div>

              <div className="space-y-2">
                <Label>Role</Label>
                <div className="grid grid-cols-1 gap-2">
                  {(Object.keys(roleConfig) as UserRole[]).map((roleKey) => {
                    const config = roleConfig[roleKey]
                    const RoleIcon = config.icon
                    const isSelected = editForm.role === roleKey

                    return (
                      <button
                        key={roleKey}
                        type="button"
                        onClick={() => setEditForm({ ...editForm, role: roleKey })}
                        className={`flex items-center gap-3 p-3 rounded-lg border-2 transition-colors text-left ${
                          isSelected 
                            ? "border-gold bg-gold/10" 
                            : "border-saddle-light/30 hover:border-saddle-light"
                        }`}
                      >
                        <RoleIcon className={`h-5 w-5 ${isSelected ? "text-gold" : "text-saddle"}`} />
                        <div>
                          <p className="font-medium text-saddle-dark">{config.label}</p>
                          <p className="text-xs text-leather-light">{config.description}</p>
                        </div>
                      </button>
                    )
                  })}
                </div>
              </div>
            </div>
          )}
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsEditDialogOpen(false)}
              className="border-saddle-light/30"
            >
              Cancel
            </Button>
            <Button
              onClick={handleSaveEdit}
              disabled={isSaving}
              className="bg-gold hover:bg-gold-dark text-leather"
            >
              {isSaving ? "Saving..." : "Save Changes"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
