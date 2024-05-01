// cypress/integration/register.spec.js

describe('Register Page', () => {
    beforeEach(() => {
      cy.visit('http://localhost:8080/register'); // Replace '/register' with the actual URL of your register page
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
      cy.get('form input[name="Username"]').type(''); // Empty username
      cy.get('form input[name="Email"]').type('invalid-email'); // Invalid email format
      cy.get('form input[name="Password"]').type('password'); // Weak password
      cy.get('form input[name="Password2"]').type('password'); // Repeat password
  
      // Submit the form
      cy.get('form button[type="submit"]').click();
  
      // Verify that error message is displayed
      cy.get('.error').should('be.visible');
    });
  
    it('should register a new user with valid input', () => {
      // Fill the form with valid data
      cy.get('form input[name="Username"]').type('testuser');
      cy.get('form input[name="Email"]').type('test@example.com');
      cy.get('form input[name="Password"]').type('StrongPassword123');
      cy.get('form input[name="Password2"]').type('StrongPassword123');
  
      // Submit the form
      cy.get('form button[type="submit"]').click();
  
      // Verify that user is redirected to the dashboard or home page
      cy.url().should('include', '/dashboard'); // Replace '/dashboard' with expected URL after successful registration
    });
  });