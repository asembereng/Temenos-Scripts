# Temenos-Scripts Repository

Temenos-Scripts is a collection of utility scripts and automation tools related to Temenos banking software development and operations.

**ALWAYS reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Current State
This repository is currently in its initial state with minimal content:
- README.md (basic title only)  
- LICENSE (AGPL v3)
- No scripts, build systems, or automation tools yet

## Working Effectively

### Repository Bootstrap and Validation
Since this is a scripts repository without build systems, the primary validation is ensuring scripts execute correctly:

- **Repository root**: `/home/runner/work/Temenos-Scripts/Temenos-Scripts`
- **Clone and validate**: 
  ```bash
  git status
  ls -la
  ```
- **Test script execution environment**:
  ```bash
  which bash && bash --version
  which python3 && python3 --version  
  which node && node --version
  ```

### Environment Details (Validated)
- **Git**: 2.51.0 - available at `/usr/bin/git`
- **Bash**: 5.2.21 - available at `/usr/bin/bash`
- **Python**: 3.12.3 - available at `/usr/bin/python3`
- **Node.js**: v20.19.5 - available at `/usr/local/bin/node`
- **Repository path**: `/home/runner/work/Temenos-Scripts/Temenos-Scripts`

### Script Development and Testing
When scripts are added to this repository:

- **Make scripts executable**: `chmod +x script_name.sh`
- **Test basic script execution**: 
  ```bash
  ./script_name.sh
  echo $?  # Check exit code
  ```
- **Time script execution**: `time ./script_name.sh`
- **Validate script syntax**:
  - Bash: `bash -n script_name.sh`
  - Python: `python3 -m py_compile script_name.py`
  - Node.js: `node --check script_name.js`

### Repository Structure (Current)
```
/home/runner/work/Temenos-Scripts/Temenos-Scripts/
├── .git/
├── .github/
│   └── copilot-instructions.md
├── LICENSE (AGPL v3)
└── README.md
```

## Validation Requirements

### CRITICAL Timing and Timeout Guidelines
- **Script execution timeout**: Set minimum 30 seconds timeout for any script execution
- **NEVER CANCEL**: Wait for all scripts to complete execution before declaring failure
- **Long-running scripts**: If scripts take more than 5 minutes, document expected duration
- **Build operations**: When build systems are added, use 60+ minute timeouts

### Manual Validation Scenarios
When working with scripts in this repository:

1. **Always test script execution**:
   ```bash
   ./script_name.sh --help
   ./script_name.sh [with sample parameters]
   ```

2. **Verify script output and exit codes**:
   ```bash
   ./script_name.sh && echo "SUCCESS" || echo "FAILED"
   ```

3. **Test error handling**:
   ```bash
   ./script_name.sh invalid_input
   echo "Exit code: $?"
   ```

4. **Check file permissions and accessibility**:
   ```bash
   ls -la script_name.sh
   file script_name.sh
   ```

### Pre-commit Validation
Before committing changes:
- **Test all modified scripts**: Execute each script with sample inputs
- **Check syntax**: Run syntax validation for all script types
- **Verify permissions**: Ensure scripts have executable permissions
- **Test error cases**: Verify scripts handle invalid inputs gracefully

## Common Tasks and Commands

### Repository Management
```bash
# Check repository status
git status

# View repository contents  
ls -la

# Check file types
file *

# View script permissions
ls -la *.sh *.py *.js 2>/dev/null || echo "No scripts found"
```

### Script Development
```bash
# Create new bash script
echo '#!/bin/bash' > new_script.sh
chmod +x new_script.sh

# Create new Python script  
echo '#!/usr/bin/env python3' > new_script.py
chmod +x new_script.py

# Test script syntax before execution
bash -n script.sh                    # Bash syntax check
python3 -m py_compile script.py      # Python syntax check
node --check script.js               # Node.js syntax check
```

### Performance and Monitoring
```bash
# Time script execution
time ./script_name.sh

# Monitor resource usage during execution
top -p $$ &                         # Monitor current process
./script_name.sh
kill %1                              # Stop monitoring
```

## Repository Guidelines

### Script Organization
- **Location**: Place all scripts in repository root or organized subdirectories
- **Naming**: Use descriptive names with appropriate extensions (.sh, .py, .js)
- **Documentation**: Include usage comments at the top of each script
- **Error handling**: Always include proper error handling and exit codes

### Development Workflow
1. **Create script with proper shebang line**
2. **Make executable**: `chmod +x script_name`
3. **Test syntax**: Run appropriate syntax validator
4. **Test execution**: Run with sample inputs
5. **Test error cases**: Verify error handling
6. **Document usage**: Add comments and help text
7. **Commit changes**: Use descriptive commit messages

### Security Considerations
- **Input validation**: Always validate script inputs
- **Path safety**: Use absolute paths where possible
- **Permission checks**: Verify file permissions before execution
- **Sensitive data**: Never commit credentials or sensitive information

## Expected Behavior
- **Scripts execute successfully** with appropriate exit codes
- **Error messages are clear** and actionable
- **Help documentation** is available for all scripts (--help flag)
- **Resource usage** is reasonable for script operations
- **No hanging processes** or resource leaks

## Troubleshooting

### Common Issues
- **Permission denied**: Run `chmod +x script_name`
- **Command not found**: Check shebang line and interpreter availability
- **Syntax errors**: Run syntax validation commands listed above
- **Path issues**: Use absolute paths or verify current directory

### Debug Commands
```bash
# Check script execution with verbose output
bash -x script_name.sh

# Verify interpreter availability
which bash python3 node

# Check environment variables
env | grep -E "PATH|HOME|PWD"
```

This repository uses AGPL v3 license. Ensure all contributions comply with the license terms.