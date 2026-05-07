---
description: Stop and restart the WebVella.Erp.Site.TIMS dev server on http://127.0.0.1:5001
---

This workflow rebuilds and restarts the TIMS site dev server. Logs are written to `/tmp/tims-dev.log`.

1. Stop any process currently listening on port 5001 (the TIMS site).
// turbo
```bash
PIDS=$(lsof -i :5001 -sTCP:LISTEN -t)
if [ -n "$PIDS" ]; then
  kill $PIDS 2>/dev/null
  sleep 1
  PIDS=$(lsof -i :5001 -sTCP:LISTEN -t)
  if [ -n "$PIDS" ]; then kill -9 $PIDS 2>/dev/null; sleep 1; fi
fi
lsof -i :5001 -sTCP:LISTEN -t || echo "port 5001 free"
```

2. Build the TIMS site project to surface any compile errors before launching.
// turbo
```bash
dotnet build /Users/pat/repos/WebVella-ERP/WebVella.Erp.Site.TIMS/WebVella.Erp.Site.TIMS.csproj -nologo -clp:NoSummary -v:m
```

3. Start the dev server detached, listening on http://127.0.0.1:5001. Use `setsid` so the process survives the shell that spawned it.
// turbo
```bash
setsid nohup dotnet run \
  --project /Users/pat/repos/WebVella-ERP/WebVella.Erp.Site.TIMS/WebVella.Erp.Site.TIMS.csproj \
  --urls http://127.0.0.1:5001 \
  > /tmp/tims-dev.log 2>&1 < /dev/null &
disown 2>/dev/null || true
```

4. Wait for the server to start and confirm it is listening.
// turbo
```bash
for i in 1 2 3 4 5 6 7 8 9 10; do
  sleep 2
  if lsof -i :5001 -sTCP:LISTEN -t > /dev/null; then
    echo "TIMS dev server is up on http://127.0.0.1:5001 (pid=$(lsof -i :5001 -sTCP:LISTEN -t))"
    break
  fi
done
tail -20 /tmp/tims-dev.log
```

5. (Optional) Tail the log if you need to keep watching:

```bash
tail -f /tmp/tims-dev.log
```
