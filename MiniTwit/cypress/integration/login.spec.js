describe('Login Page', () => {
    beforeEach(() => {
      cy.visit('http://localhost:8080/login'); // Replace '/login' with the actual URL of your login page
    });
  
    it('should display login form', () => {
      cy.contains('Sign In').should('be.visible');
      cy.get('form').should('exist');
      cy.get('form input[name="username"]').should('exist');
      cy.get('form input[name="password"]').should('exist');
      cy.get('form input[type="submit"]').should('exist');
    });
  
    it('should display error message for invalid credentials', () => {
      // Fill the form with invalid credentials
      cy.get('form input[name="username"]').type('invalid_username');
      cy.get('form input[name="password"]').type('invalid_password');
  
      // Submit the form
      cy.get('form input[type="submit"]').click();
  
      // Verify that error message is displayed
      cy.get('.error').should('be.visible');
    });
  
    it('should login with valid credentials', () => {
      // Fill the form with valid credentials
      cy.get('form input[name="username"]').type('valid_username');
      cy.get('form input[name="password"]').type('valid_password');
  
      // Submit the form
      cy.get('form input[type="submit"]').click();
  
      // Verify that user is redirected to the dashboard or home page
      cy.url().should('include', '/dashboard'); // Replace '/dashboard' with expected URL after successful login
    });
  });