# Temenos-Scripts Repository

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Repository Overview

Temenos-Scripts is a repository for storing and organizing scripts related to Temenos banking software platform. The repository is currently in its initial state with minimal content and is structured to accommodate future script development.

## Repository Structure

The repository currently contains:
- `README.md` - Basic repository description (1 line, 17 bytes)
- `LICENSE` - GNU Affero General Public License v3 (661 lines, 34KB)
- `.github/` - GitHub configuration directory

## Working Effectively

### Initial Setup and Navigation
- Clone repository: `git clone https://github.com/asembereng/Temenos-Scripts.git`
- Navigate to repository: `cd Temenos-Scripts`
- Check repository status: `git status`
- View repository contents: `ls -la`
- View file contents: `cat README.md` or `head -10 LICENSE`

### Git Operations
- Check current branch: `git branch`
- View commit history: `git log --oneline -10`
- Check for changes: `git status`
- Stage files: `git add .`
- Commit changes: `git commit -m "Your commit message"`
- Push changes: `git push origin [branch-name]`

### Repository Information Commands
- Count lines in files: `wc -l README.md LICENSE`
- Check file types: `file README.md LICENSE`  
- Check repository size: `du -sh .`
- Find specific files: `find . -name "*.sh" -o -name "*.py" -o -name "*.md"`

## Development Guidelines

### Adding New Scripts
When adding Temenos-related scripts to this repository:

1. **Create appropriate directory structure:**
   - `scripts/` - Main scripts directory
   - `scripts/deployment/` - Deployment-related scripts
   - `scripts/maintenance/` - Maintenance scripts
   - `scripts/monitoring/` - Monitoring scripts
   - `scripts/utilities/` - General utility scripts

2. **Script naming conventions:**
   - Use descriptive names with hyphens: `deploy-temenos-app.sh`
   - Include file extensions: `.sh` for shell scripts, `.py` for Python, etc.
   - Prefix with category when helpful: `db-backup.sh`, `app-restart.sh`

3. **Script requirements:**
   - Include shebang line: `#!/bin/bash` or `#!/usr/bin/env python3`
   - Add executable permissions: `chmod +x script-name.sh`
   - Include help/usage information: `script-name.sh --help`
   - Add error handling and logging

4. **Documentation requirements:**
   - Update README.md with script descriptions and usage
   - Include inline comments in scripts
   - Document prerequisites and dependencies
   - Provide example usage

### Testing Scripts
- Test all scripts in a safe environment before committing
- Validate script functionality: `./script-name.sh --help`
- Check script syntax: `bash -n script-name.sh`
- For Python scripts: `python3 -m py_compile script-name.py`

### License Compliance
- All scripts must comply with GNU AGPL v3 license
- Include license header in script files
- Reference LICENSE file in script documentation

## Validation

### Current State Validation
The repository is currently minimal with no executable scripts. To validate the current state:

1. **Repository structure check:**
   ```bash
   ls -la
   # Should show: LICENSE, README.md, .github/
   ```

2. **File integrity check:**
   ```bash
   wc -l README.md LICENSE
   # Should show: 0 README.md, 661 LICENSE, 661 total
   ```

3. **Git status check:**
   ```bash
   git status
   # Should show clean working tree or pending changes
   ```

### Future Script Validation
When scripts are added to the repository:

1. **Syntax validation:**
   - Shell scripts: `bash -n script-name.sh`
   - Python scripts: `python3 -m py_compile script-name.py`

2. **Permissions check:**
   ```bash
   ls -la scripts/
   # Executable scripts should show 'x' permissions
   ```

3. **Help functionality:**
   ```bash
   ./script-name.sh --help
   # Should display usage information
   ```

4. **Dry run testing:**
   - Test scripts with `--dry-run` or `--test` flags when available
   - Validate in non-production environment first

## Common Tasks

### Repository Exploration
To quickly understand repository contents:
```bash
# View all files
find . -type f -not -path "./.git/*" | sort

# Check repository size
du -sh .

# View README content
cat README.md

# View license type
head -5 LICENSE
```

### Adding New Content
When adding scripts or documentation:
```bash
# Create directory structure
mkdir -p scripts/{deployment,maintenance,monitoring,utilities}

# Add new script
touch scripts/deployment/new-script.sh
chmod +x scripts/deployment/new-script.sh

# Update documentation
# Edit README.md to include new script information
```

### Maintenance Tasks
Regular repository maintenance:
```bash
# Check for large files
find . -type f -size +1M -not -path "./.git/*"

# Verify all shell scripts
find . -name "*.sh" -exec bash -n {} \;

# Check for TODO/FIXME comments
grep -r "TODO\|FIXME" . --exclude-dir=.git
```

## Important Notes

- **Current State**: Repository contains only basic files (README.md, LICENSE)
- **Future Ready**: Structure prepared for Temenos banking software scripts
- **License**: All content must comply with GNU AFFERO GENERAL PUBLIC LICENSE v3
- **No Build Process**: Currently no build system required
- **No Dependencies**: No external dependencies in current minimal state
- **No Tests**: No test framework currently present

## Troubleshooting

### Common Issues
1. **Permission denied on scripts**: Run `chmod +x script-name.sh`
2. **Git push issues**: Check branch name and remote configuration
3. **File not found**: Verify you're in the correct directory with `pwd`

### Getting Help
- View this instruction file: `cat .github/copilot-instructions.md`
- Check git status: `git status`
- View repository structure: `tree .` or `find . -type f | head -20`
- Access license information: `head -20 LICENSE`

Always validate commands and test changes in a safe environment before applying to production systems.