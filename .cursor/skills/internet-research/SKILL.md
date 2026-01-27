---
name: internet-research
description: Research topics on the internet to find similar code, projects, architectures, and patterns. Use when the user asks to research how something is built, find similar projects, look up technical implementations, search GitHub for examples, or investigate best practices for any technology or pattern.
---

# Internet Research Skill

## When to Use This Skill
Use this skill when the user asks you to:
- Research how a product/game/app is built
- Find similar open source projects on GitHub
- Look up technical architecture patterns
- Find code examples for a specific feature
- Investigate best practices for a technology
- Compare different approaches to a problem
- Find SDKs, frameworks, or tools for a use case

---

## Research Strategy: Multi-Vector Parallel Search

**Always run 3-5 searches in parallel** covering different angles. This maximizes information gathering in a single round trip.

### Standard Search Vectors

| Vector | Search Template | Purpose |
|--------|----------------|---------|
| **Architecture** | `"[topic] architecture how it works technical stack [year]"` | Understand overall system design |
| **SDK/Tools** | `"[topic] SDK framework Unity/React/etc tutorial official"` | Find official development tools |
| **Open Source** | `"GitHub [topic] open source [framework] example clone"` | Find actual code to reference |
| **Dev Talks** | `"GDC Unite conference talk [topic] developer [year]"` | Deep technical insights |
| **Documentation** | `"[topic] official documentation API reference guide"` | Authoritative sources |

### Example: Researching "Pokémon GO"

```
Parallel searches:
1. "Pokémon GO technical architecture how it was built Unity Niantic 2024"
2. "Niantic Lightship AR SDK Unity tutorial location-based game"
3. "GitHub location-based AR game Unity open source Pokemon GO clone"
4. "GDC talk Niantic Pokemon GO backend infrastructure"
```

---

## Source Prioritization

### Tier 1: Highest Value (Use First)
- **Official SDK documentation** - Actual implementation details
- **GitHub repositories** - Working code examples
- **API references** - Authoritative specifications

### Tier 2: High Value
- **GDC/Unite/conference talks** - Deep technical insights from creators
- **Engineering blogs** (company blogs, not Medium) - First-party explanations
- **Google Cloud/AWS case studies** - Infrastructure patterns

### Tier 3: Medium Value
- **Technical articles** (Medium, Dev.to) - Community explanations
- **Stack Overflow** - Specific implementation questions
- **Tutorial sites** - Step-by-step guidance

### Tier 4: Context Only
- **General news articles** - Background info only
- **Wikipedia** - Overview/history only

---

## Deep Dive Protocol

After initial search results, **fetch specific URLs** when you find:

| Finding | Action |
|---------|--------|
| GitHub repository | Fetch README, look for `/docs` folder |
| Official documentation | Fetch specific pages mentioned |
| Sample projects | Fetch the samples/examples page |
| Conference talk | Look for video link or slides |
| Engineering blog post | Fetch full article |

### Example Deep Dive

```
Initial search found: "Niantic Lightship ARDK samples"
→ Fetch: https://lightship.dev/docs/ardk/sample_projects/
→ Extract: GitHub repo link, feature list, Unity versions
```

---

## Research Modes

### Quick Scan (1-2 minutes)
**Use for**: Quick answer, "does X exist?", feature overview

```
1. Run 2-3 parallel searches
2. Summarize top results
3. Provide key links
```

### Standard Research (3-5 minutes)
**Use for**: Feature implementation, finding examples, understanding patterns

```
1. Run 4-5 parallel searches across all vectors
2. Deep fetch 2-3 most relevant URLs
3. Synthesize findings into categories
4. Provide code examples if found
5. List resources for further reading
```

### Deep Dive (5-10 minutes)
**Use for**: Architecture decisions, comprehensive comparison, building new feature

```
1. Run 5+ parallel searches
2. Deep fetch 5+ URLs
3. Extract code snippets from GitHub
4. Compare multiple approaches
5. Create detailed findings report
6. Include implementation recommendations
```

---

## Search Query Optimization

### Add Context Words
| Topic Type | Add These Terms |
|------------|-----------------|
| Game development | `Unity Unreal GDC gamedev` |
| Mobile apps | `iOS Android mobile SDK` |
| Web development | `React Vue Node API REST` |
| Backend/Infrastructure | `AWS GCP Azure Kubernetes scale` |
| AR/VR | `ARKit ARCore XR spatial` |

### Add Year for Freshness
Always include current or recent year for:
- SDK versions and APIs (changes frequently)
- Best practices (evolve over time)
- Framework comparisons (landscape shifts)

### Negative Keywords
Add `-site:pinterest.com -site:quora.com` implicitly by focusing searches on:
- `site:github.com` for code
- `site:*.dev` for documentation
- `site:medium.com` OR `site:dev.to` for articles

---

## Results Presentation Template

### Quick Summary Format
```markdown
## Research: [Topic]

### Key Findings
- **Tech Stack**: [technologies used]
- **Key Libraries**: [main dependencies]
- **Architecture**: [brief pattern description]

### Best Resources
1. [Resource name](URL) - why it's useful
2. [Resource name](URL) - why it's useful

### Relevant Code
- [Repo name](GitHub URL) - what it demonstrates
```

### Detailed Research Format
```markdown
## Research: [Topic]

### Executive Summary
[2-3 sentences on what was discovered]

### Architecture Overview
[How the system/feature is typically built]

### Technology Stack
| Layer | Technology | Purpose |
|-------|------------|---------|
| Frontend | X | ... |
| Backend | Y | ... |

### Open Source Examples
| Project | Stars | Relevance |
|---------|-------|-----------|
| [Name](URL) | ⭐ N | What it shows |

### Implementation Patterns
[Key patterns discovered with code snippets]

### Recommended Approach
[Synthesis of findings into recommendation]

### Resources for Further Reading
1. [Link] - Description
2. [Link] - Description
```

---

## Common Research Scenarios

### "How is [Product] built?"
```
Searches:
1. "[Product] technical architecture stack"
2. "[Product] engineering blog how we built"
3. "[Product company] GDC Unite talk"
4. "[Product] backend infrastructure cloud"
```

### "Find similar open source projects"
```
Searches:
1. "GitHub [feature] [framework] open source"
2. "GitHub awesome-[topic] list"
3. "[topic] clone tutorial GitHub"
4. "open source alternative to [product]"
```

### "What SDK should I use for [feature]?"
```
Searches:
1. "[feature] SDK [platform] comparison 2024"
2. "[feature] [framework] official library"
3. "[feature] best practices [platform]"
4. "GitHub [feature] [framework] production ready"
```

### "Best practices for [technology]"
```
Searches:
1. "[technology] best practices [year]"
2. "[technology] production tips lessons learned"
3. "[technology] anti-patterns mistakes avoid"
4. "[technology] official guidelines documentation"
```

---

## GitHub-Specific Research

### Finding Quality Repositories
Look for these signals:
- ⭐ **Stars**: 100+ indicates community interest
- 🔄 **Recent commits**: Active within 6 months
- 📖 **README quality**: Clear documentation
- 🧪 **Tests present**: Professional quality
- 📜 **License**: MIT/Apache for usability

### Useful GitHub Searches
```
# Find by topic + language
[topic] language:csharp stars:>50

# Find by framework
[feature] unity3d stars:>20

# Find awesome lists
awesome [topic] in:name

# Find recent activity
[topic] pushed:>2024-01-01
```

### After Finding a Repo
1. Read the README first
2. Check `/docs` or `/documentation` folder
3. Look at `/examples` or `/samples`
4. Review the main source files structure
5. Check issues for common problems

---

## Handling Research Dead Ends

### If searches return poor results:
1. **Broaden terms**: Remove specific framework names
2. **Try synonyms**: "location-based" → "GPS game" → "geolocation"
3. **Check older resources**: Remove year filter
4. **Try different platforms**: GitHub → GitLab → Bitbucket

### If topic is too niche:
1. Research the parent category instead
2. Find related technologies and adapt patterns
3. Look for academic papers (Google Scholar)
4. Check if there are Discord/Reddit communities

---

## Quick Reference Commands

| Need | Search Pattern |
|------|---------------|
| Official docs | `"[product] official documentation"` |
| GitHub examples | `site:github.com [topic] [framework]` |
| Architecture | `"[product] architecture" OR "how [product] works"` |
| Tutorials | `"[topic] tutorial [framework] [year]"` |
| Comparisons | `"[option A] vs [option B] [year]"` |
| Best practices | `"[topic] best practices production"` |
| Troubleshooting | `"[error message]" site:stackoverflow.com` |
