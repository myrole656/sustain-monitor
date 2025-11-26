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
        Schema::create('processes', function (Blueprint $table) {
            $table->id();
            
            $table->unsignedTinyInteger('initiation')->default(0);
            $table->unsignedTinyInteger('planning')->default(0);
            $table->unsignedTinyInteger('monitoring')->default(0);
            $table->unsignedTinyInteger('execution')->default(0);
            $table->unsignedTinyInteger('closing')->default(0);

            // Final grade (PLATINUM/GOLD/SILVER/CERTIFIED/FAIL)
            $table->string('status')->nullable();

        // Connect to project
        $table->foreignId('project_id')->constrained('projects')->cascadeOnDelete();
            $table->timestamps();
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('processes');
    }
};
