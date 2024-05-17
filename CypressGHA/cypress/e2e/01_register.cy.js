describe('Register Page', () => {
  beforeEach(() => {
    cy.wait(5000);
    cy.visit('/register');
  });

  it('should display sign-up form', () => {
    cy.contains('Sign Up').should('be.visible');
    cy.get('form').should('exist');
    cy.get('form input[name="Username"]').should('exist');
    cy.get('form input[name="Email"]').should('exist');
    cy.get('form input[name="Password"]').should('exist');
    cy.get('form input[name="Password2"]').should('exist');
    cy.contains('Sign Up').should('exist');
  });

  it('should display error message for invalid input', () => {
    // Fill the form with invalid data
    cy.get('form input[name="Username"]').type('test'); 
    cy.get('form input[name="Email"]').type('invalid-email'); // Invalid email format
    cy.get('form input[name="Password"]').type('password'); 
    cy.get('form input[name="Password2"]').type('password1'); // Repeat wrong password

    // Submit the form
    cy.get('form button[type="submit"]').click();

    // Verify that error message is displayed
    cy.get('.error strong').should('be.visible');
  });

  it('should register a new user with valid input', () => {
    // Fill the form with valid data
    cy.get('form input[name="Username"]').type('testUser');
    cy.get('form input[name="Email"]').type('testUser@example.com');
    cy.get('form input[name="Password"]').type('StrongPassword123');
    cy.get('form input[name="Password2"]').type('StrongPassword123');

    // Submit the form
    cy.get('form button[type="submit"]').click();

    // Verify that user is redirected to the login
    cy.url().should('include', '/Login'); 
  });
});