using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Accord.Web.Models;

public static class CommandCatalog
{
    public static IReadOnlyList<CommandGroup> Groups { get; } = Create();

    public static string Anchor(CommandGroup group) =>
        Regex.Replace(group.Name.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');

    private static List<CommandGroup> Create()
    {
        return
        [
            new CommandGroup("Profile & Participation",
                "View user profiles and server participation statistics.")
            {
                Commands =
                [
                    new Command("/profile", "Get your profile or another user's profile", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("userToLookup", "IUser", "User to look up (optional, defaults to yourself)", true, false)
                        ]
                    },
                    new Command("/leaderboard", "Get a leaderboard of XP and participation points", false, false, false),
                    new Command("/calculate-xp", "Recalculate XP for all users", true, true, false),
                ]
            },
            new CommandGroup("Reminders",
                "Set, list, and manage personal reminders.")
            {
                Commands =
                [
                    new Command("/remind me", "Add a reminder for yourself", false, false, true)
                    {
                        Parameters =
                        [
                            new Param("timeSpan", "TimeSpan", "When to be reminded (e.g. 1h, 30m)", false, false),
                            new Param("message", "string", "Reminder message content", false, false)
                        ]
                    },
                    new Command("/remind list", "List your pending reminders", false, true, false)
                    {
                        Parameters =
                        [
                            new Param("page", "int", "Page number (default: 1)", true, false)
                        ]
                    },
                    new Command("/remind delete", "Delete one of your reminders", false, true, false)
                    {
                        Parameters =
                        [
                            new Param("reminderId", "int", "ID of the reminder to delete", false, false)
                        ]
                    },
                    new Command("/remind delete-all", "Delete all your reminders", false, true, false),
                    new Command("/remind list-user", "List another user's reminders", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("member", "IGuildMember", "User to look up", false, false),
                            new Param("page", "int", "Page number (default: 1)", true, false)
                        ]
                    },
                    new Command("/remind delete-user", "Delete a specific user's reminder", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("guildMember", "IGuildMember", "User whose reminder to delete", false, false),
                            new Param("reminderId", "int", "ID of the reminder to delete", false, false)
                        ]
                    },
                    new Command("/remind delete-user-all", "Delete all reminders for a user", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("guildMember", "IGuildMember", "User whose reminders to clear", false, false)
                        ]
                    },
                ]
            },
            new CommandGroup("Tags",
                "Create, edit, search, and manage reusable content tags.")
            {
                Commands =
                [
                    new Command("/tag add", "Add a new tag", false, false, true),
                    new Command("/tag edit", "Edit an existing tag", false, false, true)
                    {
                        Parameters =
                        [
                            new Param("name", "string", "Name of the tag to edit", false, false)
                        ]
                    },
                    new Command("/tag delete", "Delete a tag", false, true, false)
                    {
                        Parameters =
                        [
                            new Param("name", "string", "Name of the tag to delete", false, false)
                        ]
                    },
                    new Command("/tag alias-add", "Add an alias to a tag", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("name", "string", "Tag name", false, false),
                            new Param("newAlias", "string", "Alias to add", false, false)
                        ]
                    },
                    new Command("/tag alias-delete", "Remove an alias from a tag", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("name", "string", "Alias to remove", false, false)
                        ]
                    },
                    new Command("/tag search", "Search tags by name or content", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("searchTerm", "string", "Search query", false, false)
                        ]
                    },
                    new Command("/tag info", "Get detailed information about a tag", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("name", "string", "Tag name", false, false)
                        ]
                    },
                ]
            },
            new CommandGroup("History & Moderation",
                "View and manage user history records, notes, and warnings.")
            {
                Commands =
                [
                    new Command("/note", "Add a private note to a user's history", false, true, false)
                    {
                        Parameters =
                        [
                            new Param("user", "IUser", "User to note", false, false),
                            new Param("content", "string", "Note content (rest of message)", false, true)
                        ]
                    },
                    new Command("/warn", "Warn a user and optionally announce in channel", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("user", "IUser", "User to warn", false, false),
                            new Param("content", "string", "Warning reason", false, false),
                            new Param("announce", "bool", "Announce warning publicly (default: true)", true, false)
                        ]
                    },
                    new Command("/history get", "Get a single history record by ID", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("noteId", "int", "History record ID", false, false)
                        ]
                    },
                    new Command("/history delete", "Delete a history record", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("noteId", "int", "History record ID", false, false)
                        ]
                    },
                    new Command("/history list", "List all history records for a user", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("user", "IUser", "User to look up", false, false)
                        ]
                    },
                ]
            },
            new CommandGroup("Channel Flags",
                "Assign or remove flags that control bot behaviour per channel.")
            {
                Commands =
                [
                    new Command("/channel-flag add", "Add a flag to a channel", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("type", "string", "Flag type (e.g. IgnoredFromXp, JoinLeaveLogs)", false, false),
                            new Param("channel", "IChannel", "Target channel (optional, defaults to current)", true, false)
                        ]
                    },
                    new Command("/channel-flag remove", "Remove a flag from a channel", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("type", "string", "Flag type to remove", false, false),
                            new Param("channel", "IChannel", "Target channel (optional, defaults to current)", true, false)
                        ]
                    },
                ]
            },
            new CommandGroup("Permissions",
                "Grant or revoke bot permissions for users and roles.")
            {
                Commands =
                [
                    new Command("/permission adduser", "Grant a permission to a user", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("member", "IGuildMember", "User to grant permission to", false, false),
                            new Param("type", "string", "Permission type (e.g. ManageFlags, ManageTags)", false, false)
                        ]
                    },
                    new Command("/permission addrole", "Grant a permission to a role", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("role", "IRole", "Role to grant permission to", false, false),
                            new Param("type", "string", "Permission type", false, false)
                        ]
                    },
                ]
            },
            new CommandGroup("AI Subscriptions",
                "Free Trial of Thought: 5 quotes/day. Load-Bearing Plus: 20, permanently coming soon. " +
                "Unnecessarily Pro: 100, imaginary billing unavailable. Only the free plan can be used. " +
                "Allowances reset at midnight UTC. Users start with no banked resets; administrators can grant them for manual quota refills. " +
                "Subscriptions, usage, and banked resets are in memory and disappear on restart. No payments or actual AI.")
            {
                Commands =
                [
                    new Command("/ai", "Get a random AI pushback quote; first use enrolls you automatically. Five successful quotes per day, shared with !ai, then a public exhaustion message.", false, false, false),
                    new Command("!ai", "Get a random AI pushback quote using the same subscription, daily quota, and thinking delay as /ai", false, false, false),
                    new Command("/ai-subscription usage", "Show your or another user's plan, used and remaining quotes, banked resets, and next UTC reset in this server without enrolling them", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("user", "IUser", "User to look up (optional, defaults to yourself)", true, false)
                        ]
                    },
                    new Command("/ai-subscription use-reset", "Spend one banked reset to restore your exhausted daily quota for /ai and !ai", false, false, false),
                    new Command("/ai-subscription grant-reset", "Grant an enrolled user one banked reset in this server without resetting daily usage", true, false, false)
                    {
                        Parameters =
                        [
                            new Param("user", "IUser", "User who should receive one banked AI reset", false, false)
                        ]
                    },
                    new Command("/ai-subscription reset", "Reset a user's daily usage in this server for both /ai and !ai, preserving their plan and banked resets", true, false, false)
                    {
                        Parameters =
                        [
                            new Param("user", "IUser", "User whose daily AI quota should be reset", false, false)
                        ]
                    },
                    new Command("/ai-subscription plans", "Browse plans and their unavailable upgrades", false, false, false),
                    new Command("/ai-subscription upgrade", "Receive a joke rejection from billing; changes nothing", false, false, false),
                ]
            },
            new CommandGroup("Utilities",
                "C# evaluation, code compilation, URL shortening, and bot configuration.")
            {
                Commands =
                [
                    new Command("!eval", "Compile and run C# using the custom REPL, with NuGet package support. Also available as !exec and !e.", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("code", "C#", "Code to evaluate; code blocks are supported", false, true)
                        ]
                    },
                    new Command("/shorten", "Shorten a URL using the configured link shortener", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("url", "string", "URL to shorten (rest of message)", false, true)
                        ]
                    },
                    new Command("/godbolt", "Compile code via Godbolt compiler explorer", false, false, true),
                    new Command("/configure", "Update a bot configuration option", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("type", "string", "Configuration option name", false, false),
                            new Param("value", "string", "New value", false, false)
                        ]
                    },
                    new Command("/close", "Mark the current help forum thread as answered", false, true, false),
                    new Command("/github post-challenge", "Post a coding challenge from a GitHub repo", true, false, false)
                    {
                        Parameters =
                        [
                            new Param("readmeUrl", "string", "URL to the challenge README", false, false)
                        ]
                    },
                ]
            },
        ];
    }

    public sealed record CommandGroup(string Name, string? Description = null)
    {
        public List<Command> Commands { get; init; } = [];
    }

    public sealed record Command(string Usage, string Description, bool IsAdmin, bool IsEphemeral, bool IsModal)
    {
        public List<Param> Parameters { get; init; } = [];
    }

    public sealed record Param(string Name, string Type, string Description, bool IsOptional, bool IsGreedy);
}

