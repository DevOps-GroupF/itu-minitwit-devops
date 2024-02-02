# itu-minitwit-devops

## Exercise session 1 - steps to update

1. Install Python 3 on server
2. Install 2to3 on server
3. Use 2to3 to convert `minitwit.py` to Python 3
4. Install flask on server
5. Update the import of `werkzeug` to `werkzeug.security`
6. Update line 42 of `minitwit.py` with `.decode` so that the database initialization works.
7. Append `.decode()` lots (27) of places (anywhere we have a `rv.data`)
8. Install SQLite dev tools on the server
    - `libsqlite3-dev` on Debian
9. Compile `flag_tool` with the makefile
10. Modify `control.sh` to change the called Python version


