

I created this bot so that people posting to reddit.com/r/boxing or reddit.com/r/mma could request a fighter's 
professional record.

The basic idea is that this bot uses the reddit API via the RedditSharp library.  It also 
takes advantage of the wikipedia API, which is where we get the fighter's record from.



____________________________________USAGE_______________________________________________________________


Make a reply to a thread in the r/boxing subreddit, in this format:

/u/RedditFighterBot <fighter name>, <fighter name> <row count>

- one or more comma separated <fighter name>, where <fighter name> is the name of a fighter with a wikipedia article 
  with a professional record table
- <row count> is an optional paramter for the number of fights to display and is an integer 0 < x < 150.  If the specified row count is
  too large, the number will automatically be scaled down to the largest number possible given the number of fighters requested.







___________________________________TO-DO_______________________________________________________________

- deploy to rpi

- create a deployment script so I can deploy from my workstation to the pi programatically