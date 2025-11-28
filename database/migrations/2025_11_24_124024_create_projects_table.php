<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    /**
     * Run the migrations.
     */
    public function up(): void
    {
        Schema::create('projects', function (Blueprint $table) {
            $table->id();
            $table->string('project_name');

            $table->string('project_location')->nullable();

            $table->date('reg_date')->nullable();

            $table->string('pic_name')->nullable(); 
            $table->string('pic_contact')->nullable(); 

            $table->string('target')->nullable();      

            // Allow only "approved" or "pending"
              $table->enum('status', ['approved', 'pending', 'none'])->default('none');

            $table->foreignId('user_id')
                  ->constrained('users')
                  ->cascadeOnDelete();
            
            $table->timestamps();
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('projects');
    }
};
