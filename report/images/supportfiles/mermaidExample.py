import subprocess

def generate_sequence_diagram(mermaid_code, output_file):
    try:
        # Write Mermaid code to a temporary file
        with open("temp.mmd", "w") as f:
            f.write(mermaid_code)
        
        # Call Mermaid CLI to generate the diagram
        subprocess.run(["mmdc", "-i", "temp.mmd", "-o", output_file], check=True)
        
        print("Sequence diagram generated successfully!")
    except subprocess.CalledProcessError as e:
        print("Error generating sequence diagram:", e)
    finally:
        # Clean up temporary file
        subprocess.run(["rm", "temp.mmd"])

# Example Mermaid code
sample = """
sequenceDiagram
    participant Alice
    participant Bob
    Alice->>+Bob: Hello Bob, how are you?
    Bob-->>-Alice: I'm good, thanks! How about you?
"""


register_sequence = """
sequenceDiagram
    participant User
    participant RegisterController
    participant Validator
    participant MiniTwitContext
    participant Database

    User->>+RegisterController: Fill register form and click register

    RegisterController->>+Validator: Validate(username, email, password, password2)
    Validator-->>-RegisterController: inputs successfull
    RegisterController->>+MiniTwitContext: Users.FirstOrDefaultAsync(username) 
    MiniTwitContext->>+Database: check if username exits in database
    Database-->>-MiniTwitContext: Results
    MiniTwitContext-->>-RegisterController: Response

    RegisterController->>+MiniTwitContext: Users.AddAsync(username, email, hash(password))
    MiniTwitContext->>+Database: Insert(username, email, hash(password))
    Database-->>-MiniTwitContext: Successfull insert
    
    MiniTwitContext-->>-RegisterController: Success
    RegisterController-->>-User: Response 200, User created
"""

login_sequence = """
sequenceDiagram
    participant User
    participant LoginController
    participant MiniTwitContext
    participant Database
    participant IPasswordHasher
    
    User->>+LoginController: Fill login form and click login    
    LoginController->>+MiniTwitContext: Users.FirstOrDefaultAsync(username) 
    MiniTwitContext->>+Database: check if username exits in database
    Database-->>-MiniTwitContext: Results
    MiniTwitContext-->>-LoginController: Response

    LoginController->>+IPasswordHasher: VerifyHashedPassword(username, storedpassword, fieldpassword) 
    IPasswordHasher-->>-LoginController: Response
    LoginController-->>-User: User login and redirect to front page
    
"""

twit_sequence = """
sequenceDiagram
    
    participant User
    participant HomeController
    participant Validator
    participant MiniTwitContext
    participant Database

    User->>+HomeController: AddMessage(tweet)
    
    HomeController->>+Validator: ValidateUser 
    Validator-->>-HomeController: Success, User is log in
    
    HomeController->>+Validator: ValidateMessageLenght(tweet)
    Validator-->>-HomeController: Success, lenght < 200
    
    HomeController->>+MiniTwitContext: Twits.AddAsync(newTwit)
    MiniTwitContext->>+Database: Insert(newTwit)
    Database-->>-MiniTwitContext: Success

    MiniTwitContext-->>-HomeController: Success

    HomeController-->>-User: response: "Your message was recorded"
  
"""

follow_sequence = """
sequenceDiagram
    
    participant User
    participant UserTimelineController
    participant Validator
    participant MiniTwitContext
    participant Database
    
    User->>+UserTimelineController: Follow(username)
    
    UserTimelineController->>+MiniTwitContext: Users.FirstOrDefaultAsync(username) 
    
    MiniTwitContext->>+Database: Search for username 
    Database-->>-MiniTwitContext: Return response
    MiniTwitContext-->>-UserTimelineController: Return username User object 

    UserTimelineController->>+MiniTwitContext: Users.FirstOrDefaultAsync(whom) 
    MiniTwitContext->>+Database: Search for user to follow username
    Database-->>-MiniTwitContext: Return response

    MiniTwitContext-->>-UserTimelineController: Return the Follower user object

    UserTimelineController->>+Validate: ValidateUser(whom) 

    Validate-->>-UserTimelineController: Return successfull if user is login

    UserTimelineController->>+MiniTwitContext: Followers.Add(newFollower);
    MiniTwitContext->>+Database: Insert(newFollower);
    
    Database-->>-MiniTwitContext: Return response
    
    MiniTwitContext-->>-UserTimelineController: Return response
    
    UserTimelineController-->>-User: Follow succesfully  
    
    
"""

generate_sequence_diagram(register_sequence, "register_sequence.png")
generate_sequence_diagram(login_sequence, "login_sequence.png")
generate_sequence_diagram(twit_sequence, "twit_sequence.png")
generate_sequence_diagram(follow_sequence, "follow_sequence.png")
